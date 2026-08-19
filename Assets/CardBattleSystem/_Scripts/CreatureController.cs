using UnityEngine;
using TMPro;

namespace Assets._Scripts
{
    public class CreatureController : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI healthText;
        public TextMeshProUGUI attackText;
        public TextMeshProUGUI nameText;

        private int _cardId;
        public int CardId => _cardId;

        public void Initialize(int cardId, string cardName, int health, int attack)
        {
            _cardId = cardId;
            if (nameText != null) nameText.text = cardName;
            UpdateStats(health, attack);
        }

        public void UpdateStats(int health, int attack)
        {
            if (healthText != null) healthText.text = $"HP: {health}";
            if (attackText != null) attackText.text = $"ATK: {attack}";
        }

        private void Update()
        {
            // Simple billboarding to look at the main camera
            if (Camera.main != null)
            {
                transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up);
            }
        }
    }
}
