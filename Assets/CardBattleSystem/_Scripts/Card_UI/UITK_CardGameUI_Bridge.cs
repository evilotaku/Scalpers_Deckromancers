using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using BattleCardGameFramework;
using csbcgf;
using System.Linq;

namespace Assets._Scripts
{
    [RequireComponent(typeof(UITK_CardGameUI))]
    public class UITK_CardGameUI_Bridge : MonoBehaviour
    {
        [Header("References")]
        public CardBattleManager gameManager;
        public BoardStateManager boardStateManager;
        public VisualTreeAsset cardTemplate;

        [Header("Static Data")]
        public List<csbcgf.CardStaticData> cardDataList = new List<csbcgf.CardStaticData>();
        private Dictionary<string, csbcgf.CardStaticData> _staticDataDict = new Dictionary<string, csbcgf.CardStaticData>();

        private UITK_CardGameUI _cardGameUI;
        private UIDocument _uiDocument;
        private VisualElement _root;

        private VisualElement _playerHand;
        private VisualElement _playerBoard;

        private VisualElement _opponentHand;
        private VisualElement _opponentBoard;

        private Button _endTurnButton;

        // Stores a list of card IDs that were built in the hand on the last state update.
        // This lets us detect when a slot containing one of these hand card IDs gets dropped into gridLayout.
        private HashSet<int> _handCardIds = new HashSet<int>();

        private void Awake()
        {
            _cardGameUI = GetComponent<UITK_CardGameUI>();
            _uiDocument = GetComponent<UIDocument>();
            InitializeStaticData();
        }

        private void InitializeStaticData()
        {
            cardDataList = Resources.LoadAll<CardStaticData>("Data/Cards/StaticData").ToList();
            _staticDataDict.Clear();
            foreach (var data in cardDataList)
            {
                if (data != null && !string.IsNullOrEmpty(data.name))
                {
                    // Map both the name and a version without "StaticData" if it exists
                    string key = data.name;
                    if (key.EndsWith("StaticData")) key = key.Substring(0, key.Length - 10);
                    _staticDataDict[key] = data;
                }
            }
        }

        private void OnEnable()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<CardBattleManager>();
            }

            if (boardStateManager == null)
            {
                boardStateManager = FindAnyObjectByType<BoardStateManager>();
            }

            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated += HandleGameStateUpdated;
            }

            CardDragManipulator.OnItemDropped += HandleItemDropped;
            CardDragManipulator.OnWorldDropped += HandleWorldDropped;
            
            // Set up initial elements
            if (_uiDocument != null)
            {
                _root = _uiDocument.rootVisualElement;
                if (_root != null)
                {
                    _playerHand = _root.Q<VisualElement>(className:"player-hand");
                    _playerBoard = _root.Q<VisualElement>(className: "player-board");

                    _opponentHand = _root.Q<VisualElement>(className: "opponent-hand");
                    _opponentBoard = _root.Q<VisualElement>(className: "opponent-board");
                }
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateUpdated -= HandleGameStateUpdated;
            }

            CardDragManipulator.OnItemDropped -= HandleItemDropped;
            CardDragManipulator.OnWorldDropped -= HandleWorldDropped;
        }

        private void HandleGameStateUpdated(BaseGameClientStateDTO baseState)
        {
            var state = baseState.AsHearthstoneState();
            if (state == null) return;



            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _playerHand = _root.Q<VisualElement>("PlayerHand")?.Q<VisualElement>(className: "reorderable-list");
            _playerBoard = _root.Q<VisualElement>("PlayerBoard")?.Q<VisualElement>(className: "reorderable-list");
            _opponentHand = _root.Q<VisualElement>("OpponentHand")?.Q<VisualElement>(className: "reorderable-list");
            _opponentBoard = _root.Q<VisualElement>("OpponentBoard")?.Q<VisualElement>(className: "reorderable-list");
            _endTurnButton = _root.Q<Button>("endTurnButton");

            // Clear old children if elements exist
            if (_playerHand != null) _playerHand.Clear();
            if (_opponentHand != null) _opponentHand.Clear();
            if (_playerBoard != null) _playerBoard.Clear();
            if (_opponentBoard != null) _opponentBoard.Clear();
            _handCardIds.Clear();

            // Populate hand
            if (_playerHand != null && state.YourState != null && state.YourState.Hand != null)
            {
                foreach (var cardDto in state.YourState.Hand)
                {
                    bool canAfford = state.YourState.Mana >= cardDto.ManaCost;
                    VisualElement slot = CreateCardSlot(cardDto, isInteractive: true, canAfford: canAfford);
                    _playerHand.Add(slot);
                    _handCardIds.Add(cardDto.Id);
                }
            }

            // Populate player board cards
            if (_playerBoard != null && state.YourState != null && state.YourState.Board != null)
            {
                foreach (var cardDto in state.YourState.Board)
                {
                    VisualElement slot = CreateCardSlot(cardDto, isInteractive: true, isOpponentCard: false, canAfford: true);
                    _playerBoard.Add(slot);
                }
            }

            // Populate opponent board cards
            if (_opponentBoard != null && state.OpponentState != null && state.OpponentState.Board != null)
            {
                foreach (var cardDto in state.OpponentState.Board)
                {
                    VisualElement slot = CreateCardSlot(cardDto, isInteractive: false, isOpponentCard: true, canAfford: true);
                    _opponentBoard.Add(slot);
                }
            }
            if (_opponentHand != null && state.OpponentState != null && state.OpponentState.Hand != null)
            {
                foreach (var cardDto in state.OpponentState.Hand)
                {
                    VisualElement slot = CreateCardSlot(cardDto, isInteractive: false, isOpponentCard: true, canAfford: true);
                    _opponentHand.Add(slot);
                }
            }

            if (_endTurnButton != null)
            {
                _endTurnButton.clicked += async () =>
                {
                    await gameManager?.EndTurnAsync();
                };
            }
            // Refresh layout fanning
            if (_cardGameUI != null)
            {
                _cardGameUI.UpdateHorizontalFan();
                _cardGameUI.ResetSlotsInOtherLists();
            }
        }

        private void HandleWorldDropped(VisualElement cardContent, Vector2 screenPosition)
        {
            if (cardContent == null || cardContent.userData is not int cardId) return;

            // Only allow playing cards from our hand
            if (!_handCardIds.Contains(cardId)) return;

            // Perform Raycast from FPS Camera
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // Flip Y for ScreenPointToRay since UI Toolkit and Input System use different conventions sometimes
            // But ScreenPointToRay usually expects screen space (pixels)
            Ray ray = mainCam.ScreenPointToRay(new Vector3(screenPosition.x, Screen.height - screenPosition.y, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"Card {cardId} dropped on world at {hit.point}");

                if (boardStateManager != null)
                {
                    boardStateManager.SetSpawnPositionForCard(cardId, hit.point);
                }

                if (gameManager != null)
                {
                    _ = gameManager.PlayCardAsync(cardId, "");
                }
            }
        }

        private void HandleItemDropped(VisualElement cardContent, VisualElement originalSlot)
{
            if (cardContent == null || originalSlot == null) return;

            // Retrieve card ID from userData
            if (cardContent.userData is int cardId)
            {
                // Verify if this card was originally in our hand, and is now placed inside the play area
                if (_handCardIds.Contains(cardId))
                {
                    VisualElement currentSlot = cardContent.parent;
                    if (currentSlot != null && currentSlot.parent != null && currentSlot.parent == _playerBoard)
                    {
                        Debug.Log($"Bridge detected Card ID {cardId} played from hand to board!");
                        
                        // Notify MultiplayerGameManager
                        if (gameManager != null)
                        {
                            _ = gameManager.PlayCardAsync(cardId, "");
                        }
                    }
                }
            }
        }

        private VisualElement CreateCardSlot(CardClientStateDTO dto, bool isInteractive, bool isOpponentCard = false, bool canAfford = true)
        {
            // 1. Create slot (.card-slot)
            VisualElement slot = new VisualElement();
            slot.AddToClassList("card-slot");

            // 2. Create content (.card-content)
            VisualElement content = new VisualElement();
            content.AddToClassList("card-content");
            content.userData = dto.Id; // Store card ID for matching played cards!

            if (!canAfford)
            {
                content.AddToClassList("card-unplayable");
            }

            if (isOpponentCard)
            {
                // Give opponent cards a red target tint border or style
                var redColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);
                content.style.borderLeftColor = redColor;
                content.style.borderTopColor = redColor;
                content.style.borderRightColor = redColor;
                content.style.borderBottomColor = redColor;
                content.style.borderLeftWidth = 2f;
                content.style.borderTopWidth = 2f;
                content.style.borderRightWidth = 2f;
                content.style.borderBottomWidth = 2f;
            }

            // 3. Instantiate template and append
            if (cardTemplate != null)
            {
                VisualElement cardUI = cardTemplate.Instantiate();
                cardUI.pickingMode = PickingMode.Ignore;
                cardUI.style.paddingBottom = 177;
                
                // Set text values dynamically
                BindCardData(cardUI, dto);
                
                content.Add(cardUI);
            }
            else
            {
                // Fallback label if visual template is not assigned
                Label label = new Label(dto.CardType);
                label.style.color = Color.white;
                label.style.alignSelf = Align.Center;
                content.Add(label);
            }

            slot.Add(content);

            // 4. Attach pointer manipulators if card is interactive and playable
            if (isInteractive && canAfford)
            {
                _cardGameUI.SetupCardManipulators(content);
            }

            return slot;
        }

        private void BindCardData(VisualElement cardRoot, CardClientStateDTO dto)
        {
            // Get Card Static Info Map
            csbcgf.CardStaticData info = GetCardStaticData(dto.CardType);

            if (info == null)
            {
                Debug.LogWarning($"Static data not found for card type: {dto.CardType}");
                return;
            }

            bool isHidden = info.Type == "HIDDEN";

            // If hidden, show cardback and hide all content
            if (isHidden)
            {
                // Add cardback element
                VisualElement cardback = new VisualElement();
                cardback.AddToClassList("scifi-cardback");
                cardback.pickingMode = PickingMode.Ignore;
                
                // Add to the scifi-card-root
                var root = cardRoot.Q(className: "scifi-card-root");
                if (root != null)
                {
                    root.Add(cardback);
                }
                else
                {
                    cardRoot.Add(cardback);
                }

                // Hide main content containers
                var bg = cardRoot.Q(className: "scifi-card-bg");
                if (bg != null) bg.style.display = DisplayStyle.None;

                var topLeft = cardRoot.Q(className: "scifi-top-left");
                if (topLeft != null) topLeft.style.display = DisplayStyle.None;

                var middleBar = cardRoot.Q(className: "scifi-middle-bar");
                if (middleBar != null) middleBar.style.display = DisplayStyle.None;

                var bottomBox = cardRoot.Q(className: "scifi-bottom-box");
                if (bottomBox != null) bottomBox.style.display = DisplayStyle.None;

                var typeBarElement = cardRoot.Q(className: "scifi-type-bar");
                if (typeBarElement != null) typeBarElement.style.display = DisplayStyle.None;

                return;
            }

            // Image
            var imageElement = cardRoot.Q<VisualElement>(className: "scifi-card-bg");
            if (imageElement != null && info.CardImage != null && info.CardImage.RuntimeKeyIsValid())
            {
                info.CardImage.LoadAssetAsync<Sprite>().Completed += (handle) => {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (imageElement != null)
                            imageElement.style.backgroundImage = new StyleBackground(handle.Result);
                    }
                };
            }

            // Cost
            var costLabel = cardRoot.Q<Label>(className: "text-cost");
            if (costLabel != null) costLabel.text = dto.ManaCost.ToString();

            // Level / Tier - default 2
            var levelLabel = cardRoot.Q<Label>(className: "text-level");
            if (levelLabel != null) levelLabel.text = "2"; // Default fallback tier

            // Title / Name
            var nameLabel = cardRoot.Q<Label>(className: "scifi-card-name");
            if (nameLabel != null) nameLabel.text = info.Name;

            // Attack (AV)
            var avLabel = cardRoot.Q<Label>(className: "text-av");
            if (avLabel != null) avLabel.text = dto.Attack.ToString();

            // Defense / Life (DV)
            var dvLabel = cardRoot.Q<Label>(className: "text-dv");
            if (dvLabel != null) dvLabel.text = dto.Life.ToString();

            // Hide attack / defense plaques if spell
            var plaqueAV = cardRoot.Q<VisualElement>(className: "plaque-av");
            var plaqueDV = cardRoot.Q<VisualElement>(className: "plaque-dv");
            
            if (info.Type == "SPELL" || info.Type == "HIDDEN")
            {
                if (plaqueAV != null) plaqueAV.style.display = DisplayStyle.None;
                if (plaqueDV != null) plaqueDV.style.display = DisplayStyle.None;
            }
            else
            {
                if (plaqueAV != null) plaqueAV.style.display = DisplayStyle.Flex;
                if (plaqueDV != null) plaqueDV.style.display = DisplayStyle.Flex;
            }

            // Mechanic
            var mechanicLabel = cardRoot.Q<Label>(className: "scifi-card-mechanic");
            if (mechanicLabel != null)
            {
                mechanicLabel.text = info.Mechanic;
                mechanicLabel.style.display = string.IsNullOrEmpty(info.Mechanic) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Ability Name
            var abilityNameLabel = cardRoot.Q<Label>(className: "scifi-card-ability-name");
            if (abilityNameLabel != null)
            {
                abilityNameLabel.text = info.AbilityName;
                abilityNameLabel.style.display = string.IsNullOrEmpty(info.AbilityName) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Ability Desc
            var abilityDescLabel = cardRoot.Q<Label>(className: "scifi-card-ability-desc");
            if (abilityDescLabel != null)
            {
                abilityDescLabel.text = info.AbilityDesc;
                abilityDescLabel.style.display = string.IsNullOrEmpty(info.AbilityDesc) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Story
            var storyLabel = cardRoot.Q<Label>(className: "scifi-card-story");
            if (storyLabel != null)
            {
                storyLabel.text = info.Story;
                storyLabel.style.display = string.IsNullOrEmpty(info.Story) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Type Bar Layout
            var typeBar = cardRoot.Q<VisualElement>(className: "scifi-type-bar");
            if (typeBar != null)
            {
                typeBar.style.display = (info.Type == "HIDDEN") ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Type
            var typeLabel = cardRoot.Q<Label>(className: "scifi-type-text");
            if (typeLabel != null) typeLabel.text = info.Type;

            // Class
            var classLabel = cardRoot.Q<Label>(className: "scifi-class-text");
            if (classLabel != null) classLabel.text = info.Class;

            // Race
            var raceLabel = cardRoot.Q<Label>(className: "scifi-race-text");
            if (raceLabel != null)
            {
                raceLabel.text = info.Race;
                var divider = cardRoot.Q<Label>(className: "scifi-type-divider");
                if (divider != null)
                {
                    divider.style.display = string.IsNullOrEmpty(info.Race) ? DisplayStyle.None : DisplayStyle.Flex;
                }
                raceLabel.style.display = string.IsNullOrEmpty(info.Race) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private csbcgf.CardStaticData GetCardStaticData(string cardType)
        {
            if (_staticDataDict.TryGetValue(cardType, out var data))
            {
                return data;
            }
            return null;
        }
    }
}
