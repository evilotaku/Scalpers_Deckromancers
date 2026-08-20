using Unity.Netcode;
using UnityEngine;
using Assets._Scripts;
using System.Collections.Generic;
using BattleCardGameFramework;

namespace Assets.Scripts.Dungeon
{
    public class DungeonRoom : NetworkBehaviour, IInteractable
    {
        public NetworkVariable<CardBattleManager.ModuleType> roomGameType = new NetworkVariable<CardBattleManager.ModuleType>(
            CardBattleManager.ModuleType.Hearthstone, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> isCompleted = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [SerializeField] private GameObject gameTable;
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform exitPoint;

        public Transform EntrancePoint => entrancePoint;
        public Transform ExitPoint => exitPoint;

        private bool _isGameActive;

        private void OnEnable()
        {
            var cardManager = Object.FindAnyObjectByType<CardBattleManager>();
            if (cardManager != null)
            {
                cardManager.OnGameStateUpdated += HandleGameStateUpdated;
            }
        }

        private void OnDisable()
        {
            var cardManager = Object.FindAnyObjectByType<CardBattleManager>();
            if (cardManager != null)
            {
                cardManager.OnGameStateUpdated -= HandleGameStateUpdated;
            }
        }

        public void Interact(ulong clientId)
        {
            if (isCompleted.Value) return;

            StartGameRpc(clientId);
        }

        [Rpc(SendTo.Everyone)]
        private void StartGameRpc(ulong clientId)
        {
            Debug.Log($"Room {gameObject.name}: Starting game type {roomGameType.Value} for client {clientId}");
            
            var cardManager = Object.FindAnyObjectByType<CardBattleManager>();
            if (cardManager != null)
            {
                cardManager.activeModule = roomGameType.Value;
                _isGameActive = true;
                
                // Only the local player who interacted starts the UI/game logic
                if (NetworkManager.Singleton.LocalClientId == clientId)
                {
                    _ = cardManager.StartGameAsync();
                }
            }
        }

        private void HandleGameStateUpdated(BaseGameClientStateDTO state)
        {
            if (!_isGameActive || isCompleted.Value) return;

            if (state != null && state.IsGameOver)
            {
                Debug.Log($"Room {gameObject.name}: Game Over detected.");
                _isGameActive = false;
                SetCompletedRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void SetCompletedRpc()
        {
            isCompleted.Value = true;
        }

        public void SetCompleted()
        {
            if (IsServer)
            {
                isCompleted.Value = true;
            }
        }
}
}
