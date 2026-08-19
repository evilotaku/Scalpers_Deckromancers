using csbcgf;
using System.Collections.Generic;
using System.Linq;

namespace snap
{
    public class SnapGame : Game<SnapGameState>
    {
        protected SnapGame() { }

        public SnapGame(SnapGameState state) : base(state) { }

        public void StartGame()
        {
            foreach (var player in State.SnapPlayers)
            {
                player.GetCardCollection(SnapConstants.Deck).Shuffle();
                for (int i = 0; i < 3; i++)
                {
                    Execute(new DrawCardAction(player));
                }
            }
            
            StartTurn(1);
        }

        public void StartTurn(int turn)
        {
            State.CurrentTurn = turn;
            foreach (var player in State.SnapPlayers)
            {
                // Energy = Turn number
                Execute(new ModifyEnergyAction(player, turn, turn));
                
                // Draw a card
                Execute(new DrawCardAction(player));
            }
        }

        public void ResolveTurn()
        {
            Execute(new ResolveTurnAction());
        }
    }
}