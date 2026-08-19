using UnityEngine;
using UnityEngine.UIElements;
using BattleCardGameFramework;

namespace Assets._Scripts
{
    public class TurnTimer : MonoBehaviour
    {
        [Header("References")]
        public CardBattleManager gameManager;
        public UIDocument uiDocument;

        [Header("Settings")]
        public float turnTimeLimit = 90f;

        private Label _timerLabel;
        private float _currentTimer;
        private string _lastActivePlayerId;
        private bool _isTimerRunning;

        private void OnEnable()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<CardBattleManager>();
            }

            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated += HandleGameStateUpdated;
            }

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null)
            {
                _timerLabel = uiDocument.rootVisualElement.Q<Label>("TurnTimerLabel");
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated -= HandleGameStateUpdated;
            }
        }

        private void HandleGameStateUpdated(BaseGameClientStateDTO state)
        {
            if (state == null) return;


            // Check if the active player has changed
            if (state.ActivePlayerId != _lastActivePlayerId)
            {
                ResetTimer();
                _lastActivePlayerId = state.ActivePlayerId;
            }

            if (state.IsGameOver)
            {
                _isTimerRunning = false;
                UpdateUI(0);
            }
        }

        private void ResetTimer()
        {
            _currentTimer = turnTimeLimit;
            _isTimerRunning = true;
            UpdateUI(_currentTimer);
        }

        private void Update()
        {
            if (!_isTimerRunning) return;

            if (_currentTimer > 0)
            {
                _currentTimer -= Time.deltaTime;
                if (_currentTimer < 0) _currentTimer = 0;
                
                UpdateUI(_currentTimer);
            }
            else
            {
                _isTimerRunning = false;
                // Optional: Auto-end turn if it's the local player's turn?
                // The server should handle this, but we can proactively stop.
            }
        }

        private void UpdateUI(float timeRemaining)
        {
            if (_timerLabel != null)
            {
                _timerLabel.text = Mathf.CeilToInt(timeRemaining).ToString();
                
                // Optional: Change color if time is low
                if (timeRemaining <= 10f)
                {
                    _timerLabel.style.color = Color.red;
                }
                else
                {
                    _timerLabel.style.color = Color.white;
                }
            }
            else if (uiDocument != null)
            {
                // Try to find it again if it was not found initially (e.g. UI reloaded)
                _timerLabel = uiDocument.rootVisualElement.Q<Label>("TurnTimerLabel");
            }
        }
    }
}
