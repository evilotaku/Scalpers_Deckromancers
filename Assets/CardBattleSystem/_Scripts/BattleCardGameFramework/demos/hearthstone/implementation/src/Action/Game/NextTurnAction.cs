using csbcgf;
using System.Collections.Generic;

namespace hearthstone
{
    public class NextTurnAction : csbcgf.Action<HearthstoneGameState>
    {
        protected NextTurnAction() { }

        public NextTurnAction(bool isAborted = false)
            : base(isAborted)
        {
        }

        public override void Execute(IGame<HearthstoneGameState> game)
        {
            HearthstoneGameState state = game.State;

            bool wasExecuted = game.Execute(new NextPlayerAction()).Count == 1;
            if (wasExecuted)
            {
                List<IAction> startOfTurnActions = new List<IAction>();
                foreach (HearthstonePlayer activePlayer in state.ActivePlayers)
                {
                    int manaDelta = activePlayer.GetBaseValue(StatKeys.Mana) + 1 - activePlayer.GetValue(StatKeys.Mana);
                    startOfTurnActions.Add(new ModifyManaStatAction(activePlayer, manaDelta, 1));
                    startOfTurnActions.Add(new DrawCardAction(activePlayer));
                }
                game.ExecuteSimultaneously(startOfTurnActions);
            }
        }

        public override bool IsExecutable(HearthstoneGameState gameState)
        {
            return true;
        }
    }
}
