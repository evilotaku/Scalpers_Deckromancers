using Unity.Netcode;
using UnityEngine;
using Assets._Scripts;
using System.Collections.Generic;

namespace Assets.Scripts.Dungeon
{
    public class DungeonGenerator : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject roomPrefab;
        [SerializeField] private int roomCount = 5;
        [SerializeField] private float roomSpacing = 10f;

        private List<DungeonRoom> spawnedRooms = new List<DungeonRoom>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GenerateDungeon();
            }
        }

        private void GenerateDungeon()
        {
            for (int i = 0; i < roomCount; i++)
            {
                Vector3 position = transform.position + transform.forward * i * roomSpacing;
                GameObject roomObj = Instantiate(roomPrefab, position, transform.rotation);
                
                var networkObject = roomObj.GetComponent<NetworkObject>();
                networkObject.Spawn();

                var roomScript = roomObj.GetComponent<DungeonRoom>();
                if (roomScript != null)
                {
                    // Assign random card game type
                    CardBattleManager.ModuleType randomType = (CardBattleManager.ModuleType)Random.Range(0, 3);
                    roomScript.roomGameType.Value = randomType;
                    roomObj.name = $"Room_{i}_{randomType}";
                    spawnedRooms.Add(roomScript);
                }
            }
        }
    }
}
