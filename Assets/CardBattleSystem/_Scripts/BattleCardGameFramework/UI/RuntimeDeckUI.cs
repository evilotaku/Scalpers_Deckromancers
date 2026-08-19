using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using csbcgf;

namespace BattleCardGameFramework
{
    [RequireComponent(typeof(UIDocument))]
    public class RuntimeDeckUI : MonoBehaviour
    {
        [SerializeField] private StyleSheet m_StyleSheet;

        private VisualElement m_Root;
        private ScrollView m_CardLibraryGrid;
private ScrollView m_CurrentDeckList;
        private TextField m_DeckNameField;
        private Label m_StatusLabel;
        private Label m_DeckCountLabel;

        private List<int> m_EditingDeckCardIds = new List<int>();
        private Dictionary<int, ICard> m_Library = new Dictionary<int, ICard>();

        private void OnEnable()
        {
            m_Root = GetComponent<UIDocument>().rootVisualElement;
            if (m_StyleSheet != null)
            {
                m_Root.styleSheets.Add(m_StyleSheet);
            }
            BuildLayout();
            _ = Initialize();
        }

        private void BuildLayout()
        {
            m_Root.Clear();
            m_Root.AddToClassList("root");
            
            // Left Panel: Card Library
            var libraryPanel = new VisualElement();
            libraryPanel.AddToClassList("library-panel");
            
            var libTitle = new Label("Card Library");
            libTitle.AddToClassList("title");
            libraryPanel.Add(libTitle);

            m_CardLibraryGrid = new ScrollView();
            m_CardLibraryGrid.AddToClassList("card-grid");
            libraryPanel.Add(m_CardLibraryGrid);
            
            m_Root.Add(libraryPanel);

            // Right Panel: Current Deck
            var deckPanel = new VisualElement();
            deckPanel.AddToClassList("deck-panel");

            var deckTitle = new Label("Edit Deck");
            deckTitle.AddToClassList("section-header");
            deckPanel.Add(deckTitle);

            m_DeckNameField = new TextField("Deck Name");
            m_DeckNameField.value = "My New Deck";
            deckPanel.Add(m_DeckNameField);

            m_DeckCountLabel = new Label("Cards: 0");
            m_DeckCountLabel.AddToClassList("deck-count-label");
            deckPanel.Add(m_DeckCountLabel);

            m_CurrentDeckList = new ScrollView();
            m_CurrentDeckList.AddToClassList("deck-list");
            deckPanel.Add(m_CurrentDeckList);

            var saveBtn = new Button(() => _ = SaveAndUpload()) { text = "Save & Upload Deck" };
            saveBtn.AddToClassList("primary-btn");
            deckPanel.Add(saveBtn);

            m_StatusLabel = new Label("Ready");
            m_StatusLabel.AddToClassList("status-label");
            deckPanel.Add(m_StatusLabel);

            m_Root.Add(deckPanel);
        }

        private async Task Initialize()
        {
            if (PlayerDeckService.Instance == null)
            {
                var go = new GameObject("PlayerDeckService");
                go.AddComponent<PlayerDeckService>();
            }

            await PlayerDeckService.Instance.InitializeAsync();
            m_Library = PlayerDeckService.Instance.GetLibrary();
            
            m_StatusLabel.text = $"Connected: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";
            
            RefreshLibraryGrid();
            RefreshCurrentDeckUI();
        }

        private void RefreshLibraryGrid()
        {
            m_CardLibraryGrid.Clear();
            foreach (var kvp in m_Library)
            {
                var card = kvp.Value;
                int cardId = kvp.Key;

                var cardItem = new VisualElement();
                cardItem.AddToClassList("card-item");

                var nameLabel = new Label(card.GetType().Name);
                nameLabel.AddToClassList("card-name");
                cardItem.Add(nameLabel);

                var addBtn = new Button(() => TryAddCardToDeck(cardId)) { text = "Add" };
                addBtn.AddToClassList("add-btn");
                cardItem.Add(addBtn);

                m_CardLibraryGrid.Add(cardItem);
            }
        }

        private void TryAddCardToDeck(int cardId)
        {
            int count = m_EditingDeckCardIds.Count(id => id == cardId);
            if (count >= 3)
            {
                m_StatusLabel.text = "Limit: Max 3 duplicates per card!";
                return;
            }

            m_EditingDeckCardIds.Add(cardId);
            RefreshCurrentDeckUI();
        }

        private void RemoveFromDeck(int index)
        {
            if (index >= 0 && index < m_EditingDeckCardIds.Count)
            {
                m_EditingDeckCardIds.RemoveAt(index);
                RefreshCurrentDeckUI();
            }
        }

        private void RefreshCurrentDeckUI()
        {
            m_CurrentDeckList.Clear();
            m_DeckCountLabel.text = $"Cards: {m_EditingDeckCardIds.Count}";

            for (int i = 0; i < m_EditingDeckCardIds.Count; i++)
            {
                int index = i;
                int cardId = m_EditingDeckCardIds[i];
                string cardName = m_Library.TryGetValue(cardId, out var card) ? card.GetType().Name : "Unknown Card";

                var row = new VisualElement();
                row.AddToClassList("deck-list-item");

                var nameLabel = new Label(cardName);
                nameLabel.AddToClassList("deck-item-name");
                row.Add(nameLabel);

                var removeBtn = new Button(() => RemoveFromDeck(index)) { text = "X" };
                removeBtn.AddToClassList("remove-btn");
                row.Add(removeBtn);

                m_CurrentDeckList.Add(row);
            }
        }

        private async Task SaveAndUpload()
        {
            string name = m_DeckNameField.value;
            if (string.IsNullOrEmpty(name))
            {
                m_StatusLabel.text = "Please enter a deck name!";
                return;
            }

            m_StatusLabel.text = "Uploading...";
            PlayerDeckService.Instance.SaveDeck(name, m_EditingDeckCardIds);
            await PlayerDeckService.Instance.SaveToCloudAsync();
            m_StatusLabel.text = "Deck Saved!";
        }
    }
}
