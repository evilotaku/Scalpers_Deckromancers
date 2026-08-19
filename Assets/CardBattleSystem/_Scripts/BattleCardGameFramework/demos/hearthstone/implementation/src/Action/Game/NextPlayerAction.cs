using csbcgf;
using System.Linq;
using System.Collections.Generic;

namespace hearthstone
{
    public class NextPlayerAction : csbcgf.Action<HearthstoneGameState>
    {
        protected NextPlayerAction() { }

        public NextPlayerAction(bool isAborted = false)
            : base(isAborted)
        {
        }

        public override void Execute(IGame<HearthstoneGameState> game)
        {
            HearthstoneGameState state = game.State;
            List<int> teamIds = state.Players.Select(p => p.TeamId).Distinct().OrderBy(id => id).ToList();
            int currentTeamIndex = teamIds.IndexOf(state.ActiveTeamId);
            int nextTeamIndex = (currentTeamIndex + 1) % teamIds.Count;
            int nextTeamId = teamIds[nextTeamIndex];
            
            HearthstonePlayer firstPlayerInNextTeam = (HearthstonePlayer)state.Players.First(p => p.TeamId == nextTeamId);

            game.Execute(new ModifyActivePlayerAction(firstPlayerInNextTeam));
        }

        public override bool IsExecutable(HearthstoneGameState gameState)
        {
            return true;
        }
    }
}
