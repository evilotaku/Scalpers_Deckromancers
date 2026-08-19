using BattleCardGameFramework;
using csbcgf;
using hearthstone;
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
public class HearthstoneCloudModule
{
    private readonly IPushClient m_PushClient;
    private readonly ILogger<HearthstoneCloudModule> _logger;
    private readonly ITimerService _timerService;
    private readonly IGameApiClient _gameApiClient;

    public HearthstoneCloudModule(IPushClient pushClient, ILogger<HearthstoneCloudModule> logger, ITimerService timerService, IGameApiClient gameApiClient)
    {
        m_PushClient = pushClient;
        _logger = logger;
        _timerService = timerService;
        _gameApiClient = gameApiClient;
    }

    public string GameState;
    public string activeTurnTimer;
    public float turnTimeLimit = 90f; // seconds

    private void InitializeDeck(HearthstonePlayer player)
    {
        ICardCollection deck = player.GetCardCollection(CardCollectionKeys.Deck);
        deck.Add(new Wisp());
        deck.Add(new ArgentSquire());
        deck.Add(new KingMukla());
        deck.Add(new DamageSpellCard(3));
        deck.Add(new FarSight());
    }

    [CloudCodeFunction("CreateGame")]
    public async Task<HearthstoneGameClientStateDTO> CreateGame(IExecutionContext context, string player2Id, string player1DeckName = "Default", string player2DeckName = "Default")
    {
        HearthstoneGameState gameState = new HearthstoneGameState();

        // Set up Player 1
        MultiplayerPlayer player1 = new MultiplayerPlayer(context.PlayerId);
        player1.AddStat(StatKeys.Life, new Stat(30, 30));
        player1.AddStat(StatKeys.Mana, new Stat(0, 0));
        
        var p1Deck = await GetPlayerDeck(context, context.PlayerId, player1DeckName);
        if (p1Deck != null && p1Deck.Count > 0)
        {
            ICardCollection deck = player1.GetCardCollection(CardCollectionKeys.Deck);
            foreach (var card in p1Deck) deck.Add(card);
        }
        else
        {
            InitializeDeck(player1);
        }
        gameState.AddPlayer(player1);

        // Set up Player 2
        MultiplayerPlayer player2 = new MultiplayerPlayer(player2Id);
        player2.AddStat(StatKeys.Life, new Stat(30, 30));
        player2.AddStat(StatKeys.Mana, new Stat(0, 0));
        
        var p2Deck = await GetPlayerDeck(context, player2Id, player2DeckName);
        if (p2Deck != null && p2Deck.Count > 0)
        {
            ICardCollection deck = player2.GetCardCollection(CardCollectionKeys.Deck);
            foreach (var card in p2Deck) deck.Add(card);
        }
        else
        {
            InitializeDeck(player2);
        }
        gameState.AddPlayer(player2);

        // Assign unique card IDs across all cards in both decks to fix NotImplementedException in csbcgf
int nextCardId = 1;
        foreach (var player in gameState.Players)
        {
            var deck = player.GetCardCollection(CardCollectionKeys.Deck);
            foreach (ICard card in deck.Cards)
            {
                if (card is Card concreteCard)
                {
                    concreteCard.Id = nextCardId++;
                }
            }
        }

        // Create Game
        HearthstoneGame game = new HearthstoneGame(gameState);

        // Shuffling decks
        foreach (var player in game.State.Players)
        {
            player.GetCardCollection(CardCollectionKeys.Deck).Shuffle();
        }

        // Draw starting hands (3 cards each)
        for (int i = 0; i < 3; i++)
        {
            foreach (HearthstonePlayer player in game.State.Players)
            {
                player.DrawCard(game);
            }
        }

        // Set active player to player 2 initially, so NextTurn() advances to player 1
        gameState.ActivePlayer = player2;

        // Trigger turn 1
        game.NextTurn();
        activeTurnTimer = await _timerService.RegisterTimerAsync(TimeSpan.FromSeconds(turnTimeLimit), "EndTurn");

        // Persist entire game state securely under Player 1's protected cloud save
        GameState = JsonSerializer.ToJson(game);
        try
        {
            var message = JsonSerializer.ToJson(MaskStateForPlayer(game, player2Id));
            var reply = await m_PushClient.SendPlayerMessageAsync(context, message: message, playerId: player2Id);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            _logger.LogError(ex, "Error sending message to Player: {PlayerId}", player2Id);
        }
        // Return clean masked view of the board for Player 1
        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("GetPlayerState")]
    public async Task<HearthstoneGameClientStateDTO> GetPlayerState(IExecutionContext ctx)
    {
        HearthstoneGame game = JsonSerializer.FromJson<HearthstoneGame>(GameState); 
        return MaskStateForPlayer(game, ctx.PlayerId);
    }

    [CloudCodeFunction("PlayCard")]
    public async Task<HearthstoneGameClientStateDTO> PlayCard(IExecutionContext context, int cardId, string targetId = "")
    {
        HearthstoneGame game = JsonSerializer.FromJson<HearthstoneGame>(GameState);
        MultiplayerPlayer activePlayer = (MultiplayerPlayer)game.State.ActivePlayer;
        
        if (activePlayer.PlayerId != context.PlayerId)
        {
            throw new CsbcgfException("It is not your turn!");
        }

        ICardCollection hand = activePlayer.GetCardCollection(CardCollectionKeys.Hand);
        ICard cardToPlay = hand.Cards.FirstOrDefault(c => c is Card concrete && concrete.Id == cardId);

        switch (cardToPlay)
        {
            case null:
                throw new CsbcgfException("Card not found in hand.");
            case HearthstoneMonsterCard monsterCard:
                activePlayer.SummonMonster(game, monsterCard);
                break;
            case HearthstoneTargetlessSpellCard targetlessSpell:
                activePlayer.CastSpell(game, targetlessSpell);
                break;
            case HearthstoneTargetfulSpellCard targetfulSpell:
                {
                    IStatContainer target = ResolveTarget(game, targetId);
                    if (target == null)
                    {
                        throw new CsbcgfException("Target not found for targetful spell.");
                    }
                    activePlayer.CastSpell(game, targetfulSpell, target);
                    break;
                }

            default:
                throw new CsbcgfException("Unsupported card type.");
        }

        GameState = JsonSerializer.ToJson(game);
        foreach (MultiplayerPlayer player in game.State.NonActivePlayers)
        {
            await m_PushClient.SendPlayerMessageAsync(context, JsonSerializer.ToJson(MaskStateForPlayer(game, player.PlayerId)), playerId: player.PlayerId);
        }
        
        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("Attack")]
    public async Task<HearthstoneGameClientStateDTO> Attack(IExecutionContext context, int attackerCardId, string targetId)
    {
        HearthstoneGame game = JsonSerializer.FromJson<HearthstoneGame>(GameState);
        MultiplayerPlayer activePlayer = (MultiplayerPlayer)game.State.ActivePlayer;

        if (activePlayer.PlayerId != context.PlayerId)
        {
            throw new CsbcgfException("It is not your turn!");
        }

        ICardCollection board = activePlayer.GetCardCollection(CardCollectionKeys.Board);
        HearthstoneMonsterCard attacker = board.Cards.FirstOrDefault(c => c is Card concrete && concrete.Id == attackerCardId) as HearthstoneMonsterCard;

        if (attacker == null)
        {
            throw new CsbcgfException("Attacking monster is not on your board.");
        }

        IStatContainer target = ResolveTarget(game, targetId);
        if (target == null)
        {
            throw new CsbcgfException("Attack target not found.");
        }

        attacker.Attack(game, target);

        GameState = JsonSerializer.ToJson(game);
        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("EndTurn")]
    public async Task<HearthstoneGameClientStateDTO> EndTurn(IExecutionContext context)
    {
        HearthstoneGame game = JsonSerializer.FromJson<HearthstoneGame>(GameState);
        MultiplayerPlayer activePlayer = (MultiplayerPlayer)game.State.ActivePlayer;

        if (activePlayer.PlayerId != context.PlayerId)
        {
            throw new CsbcgfException("It is not your turn!");
        }

        var timerId = await _timerService.GetTimerAsync(activeTurnTimer);
        timerId?.Cancel();
        game.NextTurn();
        activeTurnTimer = await _timerService.RegisterTimerAsync(TimeSpan.FromSeconds(turnTimeLimit), "EndTurn");


        GameState = JsonSerializer.ToJson(game);
        return MaskStateForPlayer(game, context.PlayerId);
    }

    [CloudCodeFunction("SaveGameData")]
    public async Task SaveGameData(IExecutionContext context, string key, string value)
    {
        // This function allows authorized clients (like the Editor tool) to save global game data.
        // In a production game, you might want to add checks to ensure only developers can call this.
        var setItemBody = new SetItemBody(key, value);
        await _gameApiClient.CloudSaveData.SetCustomItemAsync(context, context.AccessToken, context.ProjectId, "Global", setItemBody);
    }

    private async Task<List<ICard>> GetPlayerDeck(IExecutionContext context, string playerId, string deckName)
    {
        try
        {
            // Get Library from Global Custom Data (Game Data)
            var libraryResponse = await _gameApiClient.CloudSaveData.GetCustomItemsAsync(context, context.AccessToken, context.ProjectId, "Global", new List<string> { "CardLibrary" });
            
            // Get Decks from Player Data
            var decksResponse = await _gameApiClient.CloudSaveData.GetItemsAsync(context, context.AccessToken, context.ProjectId, playerId, new List<string> { "PlayerDecks" });
            
            if (libraryResponse?.Data == null || decksResponse?.Data == null)
            {
                return null;
            }

            string libraryJson = libraryResponse.Data.Results.FirstOrDefault(i => i.Key == "CardLibrary")?.Value?.ToString();
            string decksJson = decksResponse.Data.Results.FirstOrDefault(i => i.Key == "PlayerDecks")?.Value?.ToString();

            if (string.IsNullOrEmpty(libraryJson) || string.IsNullOrEmpty(decksJson))
            {
                return null;
            }

            // Remove potential extra quotes from string conversion if it's already a string in the JSON
            if (libraryJson.StartsWith("\"") && libraryJson.EndsWith("\"")) libraryJson = libraryJson.Substring(1, libraryJson.Length - 2).Replace("\\\"", "\"");
            if (decksJson.StartsWith("\"") && decksJson.EndsWith("\"")) decksJson = decksJson.Substring(1, decksJson.Length - 2).Replace("\\\"", "\"");

            var library = JsonSerializer.FromJson<Dictionary<int, ICard>>(libraryJson);
            var decks = JsonSerializer.FromJson<Dictionary<string, List<int>>>(decksJson);

            if (library == null || decks == null || !decks.ContainsKey(deckName))
            {
                return null;
            }

            List<int> cardIds = decks[deckName];
            List<ICard> deck = new List<ICard>();
            foreach (int id in cardIds)
            {
                if (library.TryGetValue(id, out ICard card))
                {
                    // Create a fresh instance by re-serializing/deserializing
                    string cardJson = JsonSerializer.ToJson(card);
                    ICard freshCard = JsonSerializer.FromJson<ICard>(cardJson);
                    if (freshCard != null) deck.Add(freshCard);
                }
            }

            return deck;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deck {DeckName} for player {PlayerId}", deckName, playerId);
            return null;
        }
    }



    private IStatContainer ResolveTarget(HearthstoneGame game, string targetId)
    {
        if (int.TryParse(targetId, out int targetCardId))
        {
            foreach (var player in game.State.Players)
            {
                var board = player.GetCardCollection(CardCollectionKeys.Board);
                var targetCard = board.Cards.FirstOrDefault(c => c is Card concrete && concrete.Id == targetCardId);
                if (targetCard != null)
                {
                    return targetCard;
                }
            }
        }
        else
        {
            return game.State.Players.FirstOrDefault(p => p is MultiplayerPlayer mp && mp.PlayerId == targetId);
        }

        return null;
    }


    private HearthstoneGameClientStateDTO MaskStateForPlayer(HearthstoneGame game, string callerPlayerId)
    {
        MultiplayerPlayer activePlayer = (MultiplayerPlayer)game.State.ActivePlayer;
        MultiplayerPlayer player1 = (MultiplayerPlayer)game.State.Players.ElementAt(0);
        MultiplayerPlayer player2 = (MultiplayerPlayer)game.State.Players.ElementAt(1);

        MultiplayerPlayer caller = player1.PlayerId == callerPlayerId ? player1 : player2;
        MultiplayerPlayer opponent = player1.PlayerId == callerPlayerId ? player2 : player1;

        return new HearthstoneGameClientStateDTO
        {
            IsGameOver = game.IsGameOver,
            ActivePlayerId = activePlayer.PlayerId,
            YourState = MapPlayerState(caller, false),
            OpponentState = MapPlayerState(opponent, true)
        };
    }

    private PlayerClientStateDTO MapPlayerState(MultiplayerPlayer player, bool maskHand)
    {
        var handCards = player.GetCardCollection(CardCollectionKeys.Hand).Cards;
        var boardCards = player.GetCardCollection(CardCollectionKeys.Board).Cards;
        var graveyardCards = player.GetCardCollection(CardCollectionKeys.Graveyard).Cards;

        return new PlayerClientStateDTO
        {
            PlayerId = player.PlayerId,
            Mana = player.GetValue(StatKeys.Mana),
            MaxMana = player.GetBaseValue(StatKeys.Mana),
            Life = player.GetValue(StatKeys.Life),
            MaxLife = player.GetBaseValue(StatKeys.Life),
            DeckSize = player.GetCardCollection(CardCollectionKeys.Deck).Size,
            Board = boardCards.Select(c => MapCard(c)).ToList(),
            Graveyard = graveyardCards.Select(c => MapCard(c)).ToList(),
            Hand = maskHand
                ? handCards.Select(_ => new CardClientStateDTO { Id = 0, CardType = "Hidden" }).ToList()
                : handCards.Select(c => MapCard(c)).ToList()
        };
    }

    private CardClientStateDTO MapCard(ICard card)
    {
        Card concrete = (Card)card;
        return new CardClientStateDTO
        {
            Id = concrete.Id,
            CardType = card.GetType().Name,
            ManaCost = card.GetValue(StatKeys.Mana),
            Attack = card.GetValue(StatKeys.Attack),
            Life = card.GetValue(StatKeys.Life),
            IsReadyToAttack = card is HearthstoneMonsterCard monster && monster.IsReadyToAttack
        };
    }
}



public class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        //config.Dependencies.AddScoped<IPushClient>();
        config.AddGameApiClient();
       
    }
}
