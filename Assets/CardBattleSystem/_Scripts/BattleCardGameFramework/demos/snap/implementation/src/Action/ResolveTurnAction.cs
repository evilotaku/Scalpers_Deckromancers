using csbcgf;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace snap
{
    public class ResolveTurnAction : csbcgf.Action<SnapGameState>
    {
        protected ResolveTurnAction() { }

        public ResolveTurnAction(bool isAborted = false) : base(isAborted) { }

        public override void Execute(IGame<SnapGameState> game)
        {
            SnapGameState state = game.State;

            // 1. Determine Priority
            List<SnapPlayer> players = state.SnapPlayers.ToList();
            SnapPlayer priorityPlayer = DeterminePriority(state, players[0], players[1]);
            SnapPlayer otherPlayer = players.First(p => p != priorityPlayer);

            // 2. Reveal Priority Player's cards
            RevealPendingCards(game, priorityPlayer);

            // 3. Reveal Other Player's cards
            RevealPendingCards(game, otherPlayer);

            // 4. Advance Turn or End Game
            if (state.CurrentTurn < SnapConstants.MaxTurns)
            {
                ((SnapGame)game).StartTurn(state.CurrentTurn + 1);
            }
            else
            {
                game.Execute(new GameOverEvent());
            }
        }

        private SnapPlayer DeterminePriority(SnapGameState state, SnapPlayer p1, SnapPlayer p2)
        {
            int p1Wins = 0;
            int p2Wins = 0;
            int p1TotalPower = 0;
            int p2TotalPower = 0;

            foreach (var loc in state.Locations)
            {
                int p1Power = loc.GetPower(state, p1.TeamId);
                int p2Power = loc.GetPower(state, p2.TeamId);
                p1TotalPower += p1Power;
                p2TotalPower += p2Power;

                if (p1Power > p2Power) p1Wins++;
                else if (p2Power > p1Power) p2Wins++;
            }

            if (p1Wins > p2Wins) return p1;
            if (p2Wins > p1Wins) return p2;

            if (p1TotalPower > p2TotalPower) return p1;
            if (p2TotalPower > p1TotalPower) return p2;

            // Random if absolute tie
            return new System.Random().Next(2) == 0 ? p1 : p2;
        }

        private void RevealPendingCards(IGame<SnapGameState> game, SnapPlayer player)
        {
            for (int i = 0; i < SnapConstants.NumberOfLocations; i++)
            {
                string pendingKey = SnapConstants.Pending + i;
                var pendingCards = player.GetCardCollection(pendingKey).Cards.Cast<SnapCard>().ToList();
                foreach (var card in pendingCards)
                {
                    game.Execute(new RevealCardAction(card, i));
                }
            }
        }

        public override bool IsExecutable(SnapGameState gameState)
        {
            return true;
        }
    }
}