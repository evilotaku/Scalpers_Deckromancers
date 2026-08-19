using csbcgf;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace snap
{
    public class SnapGameState : GameState
    {
        [JsonProperty]
        public List<SnapLocation> Locations { get; protected set; }

        [JsonProperty]
        public int CurrentTurn { get; set; }

        protected SnapGameState() { }

        public SnapGameState(bool _ = true) : base(_)
        {
            CurrentTurn = 1;
            Locations = new List<SnapLocation>();
            for (int i = 0; i < SnapConstants.NumberOfLocations; i++)
            {
                Locations.Add(new SnapLocation(i, "Location " + (i + 1)));
            }
        }

        [JsonIgnore]
        public IEnumerable<SnapPlayer> SnapPlayers => Players.Cast<SnapPlayer>();

        public int GetWinnerTeamId()
        {
            int p1Wins = 0;
            int p2Wins = 0;
            int p1TotalPower = 0;
            int p2TotalPower = 0;

            var playersList = Players.ToList();
            int team1Id = playersList[0].TeamId;
            int team2Id = playersList[1].TeamId;

            foreach (var loc in Locations)
            {
                int p1Power = loc.GetPower(this, team1Id);
                int p2Power = loc.GetPower(this, team2Id);

                p1TotalPower += p1Power;
                p2TotalPower += p2Power;

                if (p1Power > p2Power) p1Wins++;
                else if (p2Power > p1Power) p2Wins++;
            }

            if (p1Wins > p2Wins) return team1Id;
            if (p2Wins > p1Wins) return team2Id;

            // Tie-break: Total Power
            if (p1TotalPower > p2TotalPower) return team1Id;
            if (p2TotalPower > p1TotalPower) return team2Id;

            return -1; // Draw
        }
    }
}