using BattleCardGameFramework;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.Subscriptions;
using Unity.Services.Multiplayer;
using UnityEngine;


namespace Assets._Scripts
{
    public class CardBattleManager : MonoBehaviour
    {
        public enum ModuleType { Hearthstone, Snap, Mahjong, Dungeon }

        [Header("Module Configuration")]
        public ModuleType activeModule = ModuleType.Hearthstone;

        ISession session;  
        public string SessionId;

        public IBattleGameClient m_Client;

        public BaseGameClientStateDTO m_CurrentState;

        public event Action<BaseGameClientStateDTO> OnGameStateUpdated;
        public event Action<string> OnErrorOccurred;

        private void Awake()
        {
           OnGameStateUpdated += (state) => Debug.Log($"Game state updated. {JsonConvert.SerializeObject(state)}");
        }

        public void InitializeClient(string sessionId)
        {
            SessionId = sessionId;
            switch (activeModule)
            {
                case ModuleType.Snap:
                    m_Client = new SnapCloudModuleClient(SessionId);
                    break;
                case ModuleType.Mahjong:
                    m_Client = new MahjongCloudModuleClient(SessionId);
                    break;
                case ModuleType.Hearthstone:
                default:
                    m_Client = new HearthstoneCloudModuleClient(SessionId);
                    break;
            }
        }

        public void OnInitialized()
        {                       
            var _ = SubscribeToPlayerMessages();
            MultiplayerService.Instance.SessionAdded += (session) =>
            {               
                print("Session added: " + session.Id);
                SessionId = session.Id;
                this.session = session;
                this.session.PlayerJoined += async (player) =>
                {
                    print("Player joined: " + session.Players.First(p => p.Id == player).GetPlayerName());
                    if (session.IsHost) await StartGameAsync();
                };

                PlayerPrefs.SetString("SessionId", SessionId);
            };

            if(PlayerPrefs.GetString("SessionId", "") != "")
            {
                ReconnectGame(PlayerPrefs.GetString("SessionId"));
            };
        }

        /// <summary>
        /// Start a new game with an opponent.
        /// </summary>
        [ContextMenu("Start Game")]
        public async Task StartGameAsync()
        {
            var opponentPlayer = session.Players.FirstOrDefault(p => p.Id != AuthenticationService.Instance.PlayerId);
            Debug.Log($"Attempting to create game session {SessionId} with opponent {opponentPlayer.GetPlayerName()}...");
            var p1Deck = session.Players.FirstOrDefault(p => p.Id == AuthenticationService.Instance.PlayerId).Properties["DeckName"].Value;
            var p2Deck = opponentPlayer.Properties["DeckName"].Value;
            
            InitializeClient(SessionId);

            try
            {
                switch (m_Client)
                {
                    case HearthstoneCloudModuleClient hsClient:
                        {
                            var result = await hsClient.CreateGame(null, opponentPlayer.Id, p1Deck, p2Deck);
                            var dtoType = GetCurrentDTOType();
                            m_CurrentState = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), dtoType) as BaseGameClientStateDTO;
                            break;
                        }

                    default:
                        m_CurrentState = await m_Client.GetPlayerState();
                        break;
                }
                print("Game created successfully!");
            }
            catch (CloudCodeException e)
            {
                Debug.LogError($"Failed to create game: {e.Message}");
                OnErrorOccurred?.Invoke(e.Message);
                return;
            }
            OnGameStateUpdated?.Invoke(m_CurrentState);
        }

        public void ReconnectGame(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;
            InitializeClient(sessionId);
            _ = RefreshStateAsync();
        }

        /// <summary>
        /// Query the current game state from the cloud.
        /// </summary>
        public async Task RefreshStateAsync()
        {
            if (string.IsNullOrEmpty(SessionId) || m_Client == null) return;

            try
            {
                m_CurrentState = await m_Client.GetPlayerState();

                OnGameStateUpdated?.Invoke(m_CurrentState);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to fetch state: {e.Message}");
                OnErrorOccurred?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Play a card from hand to board, optionally targeting another card or player.
        /// </summary>
        public async Task PlayCardAsync(int cardId, string targetId = "")
        {
            if (string.IsNullOrEmpty(SessionId) || m_Client == null) return;

            try
            {
                m_CurrentState = await m_Client.PlayCard(cardId, targetId);

                OnGameStateUpdated?.Invoke(m_CurrentState);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to play card: {e.Message}");
                OnErrorOccurred?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Attack a target on the board or opponent player using an active monster.
        /// </summary>
        public async Task AttackAsync(int attackerCardId, string targetId)
        {
            if (string.IsNullOrEmpty(SessionId) || m_Client == null) return;

            try
            {
                m_CurrentState = await m_Client.Attack(attackerCardId, targetId);

                OnGameStateUpdated?.Invoke(m_CurrentState);
            }
            catch (Exception e)
            {
                Debug.LogError($"Attack action failed: {e.Message}");
                OnErrorOccurred?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// End the current player's turn.
        /// </summary>
        public async Task EndTurnAsync()
        {
            if (string.IsNullOrEmpty(SessionId) || m_Client == null) return;

            try
            {
                m_CurrentState = await m_Client.EndTurn();

                OnGameStateUpdated?.Invoke(m_CurrentState);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to end turn: {e.Message}");
                OnErrorOccurred?.Invoke(e.Message);
            }
        }

        SubscriptionEventCallbacks callbacks = new();
        public async Task SubscribeToPlayerMessages()
        {
            callbacks.ConnectionStateChanged += (state) => print($"[Cloud Code Event] Connection state changed: {state}");            
            callbacks.Kicked += () => print($"Player was kicked from session");
            callbacks.Error += (error) => print($"Error in subscription events: {error}");
            callbacks.MessageReceived += PlayerMessageRecieved;
            try
            {
                await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
            }
            catch (CloudCodeException e)
            {
                Debug.LogError($"Failed to subscribe to player messages: {e.Message}");
                OnErrorOccurred?.Invoke(e.Message);
            }
        }

        void PlayerMessageRecieved(IMessageReceivedEvent evt)
        {
            var dtoType = GetCurrentDTOType();
            var state = JsonConvert.DeserializeObject(evt.Message, dtoType) as BaseGameClientStateDTO;
            if (state != null)
            {
                m_CurrentState = state;
                OnGameStateUpdated?.Invoke(m_CurrentState);
            }
        }

        private Type GetCurrentDTOType()
        {
            switch (activeModule)
            {
                case ModuleType.Snap:
                    return typeof(BaseGameClientStateDTO).Assembly.GetType("snap.SnapGameClientStateDTO");
                case ModuleType.Mahjong:
                    return typeof(BaseGameClientStateDTO).Assembly.GetType("mahjong.MahjongGameClientStateDTO");
                case ModuleType.Hearthstone:
                default:
                    return typeof(BaseGameClientStateDTO).Assembly.GetType("BattleCardGameFramework.HearthstoneGameClientStateDTO");
            }
        }

        private void OnDestroy()
        {
            callbacks.MessageReceived -= PlayerMessageRecieved;  
        }
    }
}

