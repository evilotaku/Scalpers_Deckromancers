using UnityEngine;
using UnityEngine.UIElements;
using BattleCardGameFramework;
using Assets._Scripts;

namespace Assets._Scripts
{
    [RequireComponent(typeof(UIDocument))]
    public class FPSHUDController : MonoBehaviour
    {
        [Header("References")]
        public CardBattleManager gameManager;
        
        private VisualElement _root;
        private Label _healthValue;
        private VisualElement _healthFill;
        private Label _manaValue;
        private VisualElement _manaFill;
        private VisualElement _crosshair;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            if (_root == null) return;

            _healthValue = _root.Q<Label>("healthValue");
            _healthFill = _root.Q<VisualElement>("healthFill");
            _manaValue = _root.Q<Label>("manaValue");
            _manaFill = _root.Q<VisualElement>("manaFill");
            _crosshair = _root.Q<VisualElement>("crosshair");

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<CardBattleManager>();
            }

            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated += HandleGameStateUpdated;
                // Initial update
                if (gameManager.m_CurrentState != null)
                {
                    HandleGameStateUpdated(gameManager.m_CurrentState);
                }
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated -= HandleGameStateUpdated;
            }
        }

        private void Update()
        {
            // Toggle crosshair based on cursor state
            if (_crosshair != null)
            {
                _crosshair.style.display = UnityEngine.Cursor.lockState == CursorLockMode.Locked ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void HandleGameStateUpdated(BaseGameClientStateDTO baseState)
        {
            var state = baseState.AsHearthstoneState();
            if (state == null || state.YourState == null) return;



            var player = state.YourState;

            if (_healthValue != null)
            {
                _healthValue.text = player.Life.ToString();
            }

            if (_healthFill != null && player.MaxLife > 0)
            {
                float percent = (float)player.Life / player.MaxLife;
                _healthFill.style.width = new Length(percent * 100, LengthUnit.Percent);
            }

            if (_manaValue != null)
            {
                _manaValue.text = player.Mana.ToString();
            }

            if (_manaFill != null && player.MaxMana > 0)
            {
                float percent = (float)player.Mana / player.MaxMana;
                _manaFill.style.width = new Length(percent * 100, LengthUnit.Percent);
            }
        }
    }
}
