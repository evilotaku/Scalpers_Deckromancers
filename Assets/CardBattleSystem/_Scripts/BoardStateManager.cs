using System.Collections.Generic;
using UnityEngine;
using BattleCardGameFramework;
using csbcgf;

namespace Assets._Scripts
{
    public class BoardStateManager : MonoBehaviour
    {
        [Header("References")]
        public CardBattleManager gameManager;
        public GameObject creaturePrefab;
        public Transform boardParent;

        [Header("Spawn Settings")]
        public float spawnRadius = 5f;

        private Dictionary<int, CreatureController> _spawnedCreatures = new Dictionary<int, CreatureController>();
        private Dictionary<int, Vector3> _pendingSpawnPositions = new Dictionary<int, Vector3>();

        private void OnEnable()
{
            if (gameManager == null) gameManager = GetComponentInParent<CardBattleManager>();
            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated += HandleGameStateUpdated;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated -= HandleGameStateUpdated;
            }
        }

        private void HandleGameStateUpdated(BaseGameClientStateDTO baseState)
        {
            var state = baseState.AsHearthstoneState();
            if (state == null) return;

            HashSet<int> activeIds = new HashSet<int>();




            // Process Your Board
            if (state.YourState?.Board != null)
            {
                foreach (var card in state.YourState.Board)
                {
                    activeIds.Add(card.Id);
                    UpdateOrCreateCreature(card);
                }
            }

            // Process Opponent Board
            if (state.OpponentState?.Board != null)
            {
                foreach (var card in state.OpponentState.Board)
                {
                    activeIds.Add(card.Id);
                    UpdateOrCreateCreature(card);
                }
            }

            // Remove creatures no longer on the board
            List<int> toRemove = new List<int>();
            foreach (var id in _spawnedCreatures.Keys)
            {
                if (!activeIds.Contains(id))
                {
                    toRemove.Add(id);
                }
            }

            foreach (var id in toRemove)
            {
                Destroy(_spawnedCreatures[id].gameObject);
                _spawnedCreatures.Remove(id);
            }
        }

        private void UpdateOrCreateCreature(CardClientStateDTO card)
        {
            if (_spawnedCreatures.TryGetValue(card.Id, out var controller))
            {
                controller.UpdateStats(card.Life, card.Attack);
            }
            else
            {
                SpawnCreature(card);
            }
        }

        private void SpawnCreature(CardClientStateDTO card)
        {
            if (creaturePrefab == null) return;

            Vector3 spawnPos = Vector3.zero;
            if (_pendingSpawnPositions.TryGetValue(card.Id, out Vector3 pendingPos))
            {
                spawnPos = pendingPos;
                _pendingSpawnPositions.Remove(card.Id);
            }
            else if (boardParent != null)
            {
                spawnPos = boardParent.position + Random.insideUnitSphere * spawnRadius;
                spawnPos.y = boardParent.position.y;
            }

            GameObject go = Instantiate(creaturePrefab, spawnPos, Quaternion.identity, boardParent);
            CreatureController controller = go.GetComponent<CreatureController>();
            if (controller != null)
            {
                controller.Initialize(card.Id, card.CardType, card.Life, card.Attack);
                _spawnedCreatures.Add(card.Id, controller);
            }
        }

        public void SetSpawnPositionForCard(int cardId, Vector3 position)
        {
            if (_spawnedCreatures.TryGetValue(cardId, out var controller))
            {
                controller.transform.position = position;
            }
            else
            {
                _pendingSpawnPositions[cardId] = position;
            }
        }
}
}
