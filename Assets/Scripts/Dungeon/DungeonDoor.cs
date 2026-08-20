using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Dungeon
{
    public class DungeonDoor : NetworkBehaviour
    {
        [SerializeField] private DungeonRoom room;
        [SerializeField] private GameObject doorMesh;
        [SerializeField] private Collider doorCollider;

        private void OnEnable()
        {
            if (room != null)
            {
                room.isCompleted.OnValueChanged += OnRoomCompletedChanged;
            }
        }

        private void OnDisable()
        {
            if (room != null)
            {
                room.isCompleted.OnValueChanged -= OnRoomCompletedChanged;
            }
        }

        private void OnRoomCompletedChanged(bool oldValue, bool newValue)
        {
            UpdateDoorState(newValue);
        }

        public override void OnNetworkSpawn()
        {
            UpdateDoorState(room.isCompleted.Value);
        }

        private void UpdateDoorState(bool isOpen)
        {
            if (doorMesh != null) doorMesh.SetActive(!isOpen);
            if (doorCollider != null) doorCollider.enabled = !isOpen;
        }
    }
}
