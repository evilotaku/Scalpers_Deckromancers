using BattleCardGameFramework;
using csbcgf;
using mahjong;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Api;
using Unity.Services.CloudSave.Model;

[StateScope(Scope.MultiplayerSession)]
public class MahjongCloudModule
{
    private readonly IPushClient m_PushClient;
    private readonly ILogger<MahjongCloudModule> _logger;
    private readonly ITimerService _timerService;
    private readonly IGameApiClient _gameApiClient;

    public MahjongCloudModule(IPushClient pushClient, ILogger<MahjongCloudModule> logger, ITimerService timerService, IGameApiClient gameApiClient)
    {
        m_PushClient = pushClient;
        _logger = logger;
        _timerService = timerService;
        _gameApiClient = gameApiClient;
    }

    public string GameState;
    public string activeTurnTimer;
    public float turnTimeLimit = 60f; // seconds

    [CloudCodeFunction("SaveGameData")]
    public async Task SaveGameData(IExecutionContext context, string key, string value)
    {
        var setItemBody = new SetItemBody(key, value);
        await _gameApiClient.CloudSaveData.SetCustomItemAsync(context, context.AccessToken, context.ProjectId, "Global", setItemBody);
    }

    [CloudCodeFunction("CreateGame")]
    public async Task<MahjongGameClientStateDTO> CreateGame(IExecutionContext context, string player2Id, string player3Id, string player4Id)
    {
        MahjongGame game = new MahjongGame();
        var players = game.State.Players.ToList();
        
        // Replace default players with Multiplayer versions to store IDs
        string[] ids = { context.PlayerId, player2Id, player3Id, player4Id };
        List<IPlayer> oldPlayersList = game.State.Players.ToList();
        foreach (var p in oldPlayersList) game.State.RemovePlayer(p);

        for (int i = 0; i < 4; i++)
        {
            var oldPlayer = (MahjongPlayer)oldPlayersList[i];
            var newPlayer = new MultiplayerMahjongPlayer(ids[i]);
            
            // Transfer collections
            foreach (var key in new[] { CollectionKeys.Hand, CollectionKeys.River })
            {
                var collection = oldPlayer.GetCardCollection(key);
                newPlayer.AddCardCollection(key, collection);
            }
            
            game.State.AddPlayer(newPlayer);
        }

        // Assign unique IDs to tiles
        int nextCardId = 1;
        ICardCollection wall = game.State.GetCardCollection(CollectionKeys.Wall);
        foreach (ICard card in wall.Cards)
        {
            if (card is Card concreteCard) concreteCard.Id = nextCardId++;
        }
        foreach (var player in game.State.Players)
        {
            foreach (ICard card in player.GetCardCollection(CollectionKeys.Hand).Cards)
            {
                if (card is Card concreteCard) concreteCard.Id = nextCardId++;
            }
        }

        activeTurnTimer = await _timerService.RegisterTimerAsync(TimeSpan.FromSeconds(turnTimeLimit), "EndTurnAuto");

        GameState = JsonSerializer.ToJson(game);

        // Notify other players
        for (int i = 1; i < 4; i++)
        {
            try
            {
                await m_PushClient.SendPlayerMessageAsync(context, JsonSerializer.ToJson(MaskStateForPlayer(game, ids[i])), playerId: ids[i]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Player: {PlayerId}", ids[i]);
            }
        }

        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("GetPlayerState")]
    public MahjongGameClientStateDTO GetPlayerState(IExecutionContext context)
    {
        MahjongGame game = JsonSerializer.FromJson<MahjongGame>(GameState);
        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("DrawTile")]
    public async Task<MahjongGameClientStateDTO> DrawTile(IExecutionContext context)
    {
        MahjongGame game = JsonSerializer.FromJson<MahjongGame>(GameState);
        MultiplayerMahjongPlayer activePlayer = (MultiplayerMahjongPlayer)game.State.ActivePlayer;

        if (activePlayer.PlayerId != context.PlayerId)
        {
            throw new Exception("It is not your turn!");
        }

        game.Execute(new MahjongDrawTileAction());

        GameState = JsonSerializer.ToJson(game);
        await NotifyOtherPlayers(context, game);
        
        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("DiscardTile")]
    public async Task<MahjongGameClientStateDTO> DiscardTile(IExecutionContext context, int tileId)
    {
        MahjongGame game = JsonSerializer.FromJson<MahjongGame>(GameState);
        MultiplayerMahjongPlayer activePlayer = (MultiplayerMahjongPlayer)game.State.ActivePlayer;

        if (activePlayer.PlayerId != context.PlayerId)
        {
            throw new Exception("It is not your turn!");
        }

        ICardCollection hand = activePlayer.GetCardCollection(CollectionKeys.Hand);
        ICard tile = hand.Cards.FirstOrDefault(c => c is Card concrete && concrete.Id == tileId);

        if (tile == null)
        {
            throw new Exception("Tile not found in hand.");
        }

        game.Execute(new MahjongDiscardTileAction(tile));
        
        // Advance turn
        game.State.ActivePlayerIndex = (game.State.ActivePlayerIndex + 1) % 4;

        // Reset timer
        var timerId = await _timerService.GetTimerAsync(activeTurnTimer);
        timerId?.Cancel();
        activeTurnTimer = await _timerService.RegisterTimerAsync(TimeSpan.FromSeconds(turnTimeLimit), "EndTurnAuto");

        GameState = JsonSerializer.ToJson(game);
        await NotifyOtherPlayers(context, game);

        return MaskStateForPlayer(game, context.PlayerId);
    }

    private async Task NotifyOtherPlayers(IExecutionContext context, MahjongGame game)
    {
        foreach (MultiplayerMahjongPlayer player in game.State.Players.Cast<MultiplayerMahjongPlayer>())
        {
            if (player.PlayerId != context.PlayerId)
            {
                try
                {
                    await m_PushClient.SendPlayerMessageAsync(context, JsonSerializer.ToJson(MaskStateForPlayer(game, player.PlayerId)), playerId: player.PlayerId);
                }
                catch { }
            }
        }
    }

    private MahjongGameClientStateDTO MaskStateForPlayer(MahjongGame game, string playerId)
    {
        var allPlayers = game.State.Players.Cast<MultiplayerMahjongPlayer>().ToList();
        var caller = allPlayers.First(p => p.PlayerId == playerId);
        var opponents = allPlayers.Where(p => p.PlayerId != playerId).ToList();

        return new MahjongGameClientStateDTO
        {
            IsGameOver = false, // Simplified
            ActivePlayerId = ((MultiplayerMahjongPlayer)game.State.ActivePlayer).PlayerId,
            WallSize = game.State.GetCardCollection(CollectionKeys.Wall).Size,
            YourState = MapPlayer(caller, false),
            OpponentStates = opponents.Select(o => MapPlayer(o, true)).ToList()
        };
    }

    private MahjongPlayerClientStateDTO MapPlayer(MultiplayerMahjongPlayer player, bool maskHand)
    {
        var hand = player.GetCardCollection(CollectionKeys.Hand).Cards;
        var river = player.GetCardCollection(CollectionKeys.River).Cards;

        return new MahjongPlayerClientStateDTO
        {
            PlayerId = player.PlayerId,
            HandSize = hand.Count(),
            Hand = maskHand ? null : hand.Select(c => MapTile(c)).ToList(),
            River = river.Select(c => MapTile(c)).ToList()
        };
    }

    private MahjongTileClientStateDTO MapTile(ICard tile)
    {
        return new MahjongTileClientStateDTO
        {
            Id = ((Card)tile).Id,
            Suit = tile.GetValue(StatKeys.Suit),
            Value = tile.GetValue(StatKeys.Value)
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
