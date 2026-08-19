using BattleCardGameFramework;
using csbcgf;
using snap;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

[StateScope(Scope.MultiplayerSession)]
public class SnapCloudModule
{
    private readonly IPushClient m_PushClient;
    private readonly ILogger<SnapCloudModule> _logger;
    private readonly IGameApiClient _gameApiClient;

    public SnapCloudModule(IPushClient pushClient, ILogger<SnapCloudModule> logger, IGameApiClient gameApiClient)
    {
        m_PushClient = pushClient;
        _logger = logger;
        _gameApiClient = gameApiClient;
    }

    public string GameStateData;
    public HashSet<string> SubmittedPlayers = new HashSet<string>();

    [CloudCodeFunction("CreateGame")]
    public async Task<SnapGameClientStateDTO> CreateGame(IExecutionContext context, string player2Id)
    {
        SnapGameState gameState = new SnapGameState();

        // Set up Players
        MultiplayerSnapPlayer player1 = new MultiplayerSnapPlayer(context.PlayerId, 0);
        MultiplayerSnapPlayer player2 = new MultiplayerSnapPlayer(player2Id, 1);
        
        gameState.AddPlayer(player1);
        gameState.AddPlayer(player2);

        // Basic Decks for now
        InitializeDeck(player1);
        InitializeDeck(player2);

        // Assign IDs
        int nextId = 1;
        foreach (var p in gameState.SnapPlayers)
        {
            foreach (ICard card in p.GetCardCollection(SnapConstants.Deck).Cards)
            {
                if (card is Card concrete) concrete.Id = nextId++;
            }
        }

        SnapGame game = new SnapGame(gameState);
        game.StartGame();

        GameStateData = JsonSerializer.ToJson(game);
        SubmittedPlayers.Clear();

        // Notify Player 2
        try
        {
            await m_PushClient.SendPlayerMessageAsync(context, JsonSerializer.ToJson(MaskStateForPlayer(game, player2Id)), playerId: player2Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify Player 2");
        }

        return MaskStateForPlayer(game, context.PlayerId);
    }

    private void InitializeDeck(SnapPlayer player)
    {
        ICardCollection deck = player.GetCardCollection(SnapConstants.Deck);
        // Add some test cards (1-cost 2-power vanilla for now)
        for (int i = 0; i < 12; i++)
        {
            deck.Add(new SnapCard(1, 2));
        }
    }

    [CloudCodeFunction("PlayCard")]
    public async Task<SnapGameClientStateDTO> PlayCard(IExecutionContext context, int cardId, int locationIndex)
    {
        SnapGame game = JsonSerializer.FromJson<SnapGame>(GameStateData);
        MultiplayerSnapPlayer player = game.State.SnapPlayers.OfType<MultiplayerSnapPlayer>().FirstOrDefault(p => p.PlayerId == context.PlayerId);

        if (player == null) throw new CsbcgfException("Player not found");
        if (SubmittedPlayers.Contains(context.PlayerId)) throw new CsbcgfException("Already submitted turn");

        SnapCard card = (SnapCard)player.GetCardCollection(SnapConstants.Hand).Cards.FirstOrDefault(c => ((Card)c).Id == cardId);
        if (card == null) throw new CsbcgfException("Card not in hand");

        game.Execute(new PlayCardAction(player, card, locationIndex));

        GameStateData = JsonSerializer.ToJson(game);
        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("SubmitTurn")]
    public async Task<SnapGameClientStateDTO> SubmitTurn(IExecutionContext context)
    {
        SnapGame game = JsonSerializer.FromJson<SnapGame>(GameStateData);
        if (!game.State.SnapPlayers.Any(p => p is MultiplayerSnapPlayer mp && mp.PlayerId == context.PlayerId)) 
            throw new CsbcgfException("Invalid player");

        SubmittedPlayers.Add(context.PlayerId);

        if (SubmittedPlayers.Count >= 2)
        {
            game.ResolveTurn();
            SubmittedPlayers.Clear();
            GameStateData = JsonSerializer.ToJson(game);

            // Notify everyone
            foreach (var p in game.State.SnapPlayers)
            {
                if (p is MultiplayerSnapPlayer mp && mp.PlayerId != context.PlayerId)
                {
                    try
                    {
                        await m_PushClient.SendPlayerMessageAsync(context, JsonSerializer.ToJson(MaskStateForPlayer(game, mp.PlayerId)), playerId: mp.PlayerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to notify player {PlayerId}", mp.PlayerId);
                    }
                }
            }
        }

        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("GetPlayerState")]
    public async Task<SnapGameClientStateDTO> GetPlayerState(IExecutionContext context)
    {
        SnapGame game = JsonSerializer.FromJson<SnapGame>(GameStateData);
        return MaskStateForPlayer(game, context.PlayerId);
    }

    private SnapGameClientStateDTO MaskStateForPlayer(SnapGame game, string callerId)
    {
        var player1 = (MultiplayerSnapPlayer)game.State.Players.ElementAt(0);
        var player2 = (MultiplayerSnapPlayer)game.State.Players.ElementAt(1);

        var caller = player1.PlayerId == callerId ? player1 : player2;
        var opponent = player1.PlayerId == callerId ? player2 : player1;

        return new SnapGameClientStateDTO
        {
            IsGameOver = game.State.GetWinnerTeamId() != -1 && game.State.CurrentTurn >= SnapConstants.MaxTurns,
            CurrentTurn = game.State.CurrentTurn,
            ActivePlayerId = "", // In Snap, both are active
            YourState = MapPlayer(caller, false),
            OpponentState = MapPlayer(opponent, true),
            Locations = game.State.Locations.Select(l => new SnapLocationClientStateDTO
            {
                Index = l.Index,
                Name = l.Name,
                YourPower = l.GetPower(game.State, caller.TeamId),
                OpponentPower = l.GetPower(game.State, opponent.TeamId)
            }).ToList()
        };
    }

    private SnapPlayerClientStateDTO MapPlayer(MultiplayerSnapPlayer player, bool maskHand)
    {
        return new SnapPlayerClientStateDTO
        {
            PlayerId = player.PlayerId,
            Energy = player.GetValue(SnapConstants.Energy),
            MaxEnergy = player.GetValue(SnapConstants.MaxEnergy),
            DeckSize = player.GetCardCollection(SnapConstants.Deck).Size,
            Hand = maskHand
                ? player.GetCardCollection(SnapConstants.Hand).Cards.Select(_ => new SnapCardClientStateDTO { Id = 0, CardType = "Hidden" }).ToList()
                : player.GetCardCollection(SnapConstants.Hand).Cards.Select(c => MapCard(c)).ToList(),
            Boards = Enumerable.Range(0, 3).Select(i => player.GetCardCollection(SnapConstants.Board + i).Cards.Select(c => MapCard(c)).ToList()).ToList(),
            Pendings = Enumerable.Range(0, 3).Select(i => player.GetCardCollection(SnapConstants.Pending + i).Cards.Select(c => MapCard(c)).ToList()).ToList()
        };
    }

    private SnapCardClientStateDTO MapCard(ICard card)
    {
        SnapCard snapCard = (SnapCard)card;
        return new SnapCardClientStateDTO
        {
            Id = snapCard.Id,
            CardType = card.GetType().Name,
            Cost = card.GetValue(SnapConstants.Cost),
            Power = card.GetValue(SnapConstants.Power),
            IsRevealed = snapCard.IsRevealed
        };
    }
}

public class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();
    }
}
