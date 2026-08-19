using UnityEngine;
using UnityEngine.UIElements;
using csbcgf;
using System.Linq;
using System.Collections.Generic;

namespace mahjong
{
    [RequireComponent(typeof(UIDocument))]
    public class MahjongGameUI : MonoBehaviour
    {
        private MahjongGame game;
        private VisualElement root;
        
        private Label wallCountLabel;
        private Label activePlayerLabel;
        private Label statusLabel;
        private VisualElement riverContainer;
        private VisualElement handContainer;
        private Button drawButton;

        private void OnEnable()
        {
            game = new MahjongGame();
            root = GetComponent<UIDocument>().rootVisualElement;
            BuildLayout();
            UpdateUI();
        }

        private void BuildLayout()
        {
            root.Clear();
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;
            root.style.backgroundColor = new Color(0.1f, 0.4f, 0.2f); // Mahjong table green

            // Header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 20;

            wallCountLabel = new Label();
            wallCountLabel.style.color = Color.white;
            wallCountLabel.style.fontSize = 20;
            header.Add(wallCountLabel);

            activePlayerLabel = new Label();
            activePlayerLabel.style.color = Color.white;
            activePlayerLabel.style.fontSize = 20;
            header.Add(activePlayerLabel);

            root.Add(header);

            // Middle: River
            var riverTitle = new Label("River (Discarded Tiles):");
            riverTitle.style.color = Color.white;
            root.Add(riverTitle);

            riverContainer = new VisualElement();
            riverContainer.style.flexDirection = FlexDirection.Row;
            riverContainer.style.flexWrap = Wrap.Wrap;
            riverContainer.style.minHeight = 100;
            riverContainer.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            riverContainer.style.marginBottom = 20;
            root.Add(riverContainer);

            // Bottom: Hand
            var handTitle = new Label("Your Hand (Click to Discard):");
            handTitle.style.color = Color.white;
            root.Add(handTitle);

            handContainer = new VisualElement();
            handContainer.style.flexDirection = FlexDirection.Row;
            handContainer.style.minHeight = 80;
            handContainer.style.marginBottom = 20;
            root.Add(handContainer);

            // Footer
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.alignItems = Align.Center;

            drawButton = new Button(OnDrawClicked) { text = "Draw Tile" };
            drawButton.style.height = 40;
            drawButton.style.width = 120;
            footer.Add(drawButton);

            statusLabel = new Label();
            statusLabel.style.color = Color.yellow;
            statusLabel.style.fontSize = 24;
            statusLabel.style.marginLeft = 20;
            footer.Add(statusLabel);

            root.Add(footer);
        }

        private void UpdateUI()
        {
            MahjongGameState state = game.State;
            
            wallCountLabel.text = $"Wall: {state.GetCardCollection(CollectionKeys.Wall).Size}";
            activePlayerLabel.text = $"Active Player: {state.ActivePlayerIndex}";

            // Update River
            riverContainer.Clear();
            foreach (MahjongPlayer player in state.Players.Cast<MahjongPlayer>())
            {
                foreach (ICard tile in player.GetCardCollection(CollectionKeys.River).Cards)
                {
                    var tileLabel = new Label(GetTileString(tile));
                    tileLabel.style.width = 40;
                    tileLabel.style.height = 40;
                    tileLabel.style.backgroundColor = Color.white;
                    tileLabel.style.color = Color.black;
                    tileLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    tileLabel.style.marginRight = 5;
                    tileLabel.style.marginBottom = 5;
                    tileLabel.style.borderTopWidth = 1;
                    tileLabel.style.borderBottomWidth = 1;
                    tileLabel.style.borderLeftWidth = 1;
                    tileLabel.style.borderRightWidth = 1;
                    tileLabel.style.borderTopColor = Color.gray;
                    tileLabel.style.borderBottomColor = Color.gray;
                    tileLabel.style.borderLeftColor = Color.gray;
                    tileLabel.style.borderRightColor = Color.gray;
                    riverContainer.Add(tileLabel);
                }
            }

            // Update Hand
            handContainer.Clear();
            MahjongPlayer activePlayer = state.ActivePlayer;
            ICardCollection hand = activePlayer.GetCardCollection(CollectionKeys.Hand);
            
            // Sort hand by suit and then by value for easier play
            var sortedHand = hand.Cards.OrderBy(c => c.GetValue(StatKeys.Suit)).ThenBy(c => c.GetValue(StatKeys.Value));

            foreach (ICard tile in sortedHand)
            {
                var tileBtn = new Button(() => OnDiscardClicked(tile)) { text = GetTileString(tile) };
                tileBtn.style.width = 60;
                tileBtn.style.height = 80;
                tileBtn.style.fontSize = 16;
                handContainer.Add(tileBtn);
            }

            // Check for Win
            if (MahjongHandCalculator.IsWinningHand(hand.Cards))
            {
                statusLabel.text = "WIN!";
            }
            else
            {
                statusLabel.text = "";
            }

            // Enable/Disable Draw Button (13 is standard hand size before drawing)
            drawButton.SetEnabled(hand.Size == 13 && !state.GetCardCollection(CollectionKeys.Wall).IsEmpty);
        }

        private string GetTileString(ICard tile)
        {
            int val = tile.GetValue(StatKeys.Value);
            MahjongSuit suit = (MahjongSuit)tile.GetValue(StatKeys.Suit);
            switch (suit)
            {
                case MahjongSuit.Dots: return "D" + val;
                case MahjongSuit.Bamboos: return "B" + val;
                case MahjongSuit.Characters: return "C" + val;
                case MahjongSuit.Winds:
                    switch (val)
                    {
                        case 1: return "E";
                        case 2: return "S";
                        case 3: return "W";
                        case 4: return "N";
                    }
                    break;
                case MahjongSuit.Dragons:
                    switch (val)
                    {
                        case 1: return "R";
                        case 2: return "G";
                        case 3: return "Wh";
                    }
                    break;
            }
            return val.ToString();
        }

        private void OnDrawClicked()
        {
            game.Execute(new MahjongDrawTileAction());
            UpdateUI();
        }

        private void OnDiscardClicked(ICard tile)
        {
            game.Execute(new MahjongDiscardTileAction(tile));
            
            // Increment active player index
            game.State.ActivePlayerIndex = (game.State.ActivePlayerIndex + 1) % 4;
            
            UpdateUI();
        }
    }
}
