using UnityEngine;
using UnityEngine.UIElements;
using csbcgf;
using snap;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(UIDocument))]
public class SnapGameUI : MonoBehaviour
{
    private SnapGame _game;
    private UIDocument _uiDocument;
    private VisualElement _root;

    private VisualElement _playerHand;
    private VisualElement[] _locations = new VisualElement[3];
    private Label _energyLabel;
    private Label _turnLabel;
    private Button _endTurnButton;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        _playerHand = _root.Q<VisualElement>("PlayerHand");
        for (int i = 0; i < 3; i++)
        {
            _locations[i] = _root.Q<VisualElement>($"Location{i}");
        }

        _energyLabel = _root.Q<Label>("EnergyLabel");
        _turnLabel = _root.Q<Label>("TurnLabel");
        _endTurnButton = _root.Q<Button>("EndTurnButton");

        if (_endTurnButton != null)
        {
            _endTurnButton.clicked += OnEndTurnClicked;
        }

        // Initialize Game (Simplified for demo)
        InitializeGame();
    }

    private void InitializeGame()
    {
        SnapGameState state = new SnapGameState();
        SnapPlayer p1 = new SnapPlayer(0);
        SnapPlayer p2 = new SnapPlayer(1);
        state.AddPlayer(p1);
        state.AddPlayer(p2);

        // Add some cards to P1 deck
        for (int i = 0; i < 12; i++) p1.GetCardCollection(SnapConstants.Deck).Add(new MedusaCard());
        for (int i = 0; i < 12; i++) p2.GetCardCollection(SnapConstants.Deck).Add(new IronManCard());

        _game = new SnapGame(state);
        _game.StartGame();

        UpdateUI();
    }

    private void UpdateUI()
    {
        var player = (SnapPlayer)_game.State.Players.First(p => p.TeamId == 0);
        
        // Update Hand
        _playerHand.Clear();
        foreach (SnapCard card in player.GetCardCollection(SnapConstants.Hand).Cards)
        {
            _playerHand.Add(CreateCardUI(card));
        }

        // Update Locations
        for (int i = 0; i < 3; i++)
        {
            UpdateLocationUI(i);
        }

        _energyLabel.text = $"Energy: {player.GetValue(SnapConstants.Energy)}/{player.GetValue(SnapConstants.MaxEnergy)}";
        _turnLabel.text = $"Turn: {_game.State.CurrentTurn}/6";
    }

    private VisualElement CreateCardUI(SnapCard card)
    {
        var cardElement = new VisualElement();
        cardElement.AddToClassList("card");
        
        var nameLabel = new Label(card.GetType().Name);
        var costLabel = new Label($"C:{card.GetValue(SnapConstants.Cost)}");
        var powerLabel = new Label($"P:{card.GetValue(SnapConstants.Power)}");
        
        cardElement.Add(nameLabel);
        cardElement.Add(costLabel);
        cardElement.Add(powerLabel);

        cardElement.RegisterCallback<ClickEvent>(evt => OnCardClicked(card));

        return cardElement;
    }

    private void UpdateLocationUI(int index)
    {
        var locationElement = _locations[index];
        if (locationElement == null) return;

        var locData = _game.State.Locations[index];
        var p1Power = locData.GetPower(_game.State, 0);
        var p2Power = locData.GetPower(_game.State, 1);

        var p1Label = locationElement.Q<Label>("P1Power");
        var p2Label = locationElement.Q<Label>("P2Power");
        
        if (p1Label != null) p1Label.text = p1Power.ToString();
        if (p2Label != null) p2Label.text = p2Power.ToString();

        // Update Board Cards
        var boardElement = locationElement.Q<VisualElement>("Board");
        if (boardElement != null)
        {
            boardElement.Clear();
            var p1Board = _game.State.Players.First(p => p.TeamId == 0).GetCardCollection(SnapConstants.Board + index);
            foreach (SnapCard card in p1Board.Cards)
            {
                boardElement.Add(CreateCardUI(card));
            }
        }
    }

    private void OnCardClicked(SnapCard card)
    {
        // Simple logic: Play to middle location for now
        if (card.IsPlayable(_game.State, 1))
        {
            _game.Execute(new PlayCardAction((SnapPlayer)card.Owner, card, 1));
            UpdateUI();
        }
    }

    private void OnEndTurnClicked()
    {
        _game.ResolveTurn();
        UpdateUI();
    }
}