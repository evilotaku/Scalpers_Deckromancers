using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using csbcgf;
using Newtonsoft.Json;

namespace hearthstone
{
    public class HearthstoneGameState : GameState
    {
        [JsonProperty]
        protected int activeTeamId;
        
        protected HearthstoneGameState()
        {
        }

        public HearthstoneGameState(bool _ = true) : base(_)
        {
            this.activeTeamId = 0;
        }

        [JsonIgnore]
        public int ActiveTeamId
        {
            get => activeTeamId;
            set => activeTeamId = value;
        }

        [JsonIgnore]
        public IEnumerable<HearthstonePlayer> ActivePlayers
        {
            get => Players.Cast<HearthstonePlayer>().Where(p => p.TeamId == activeTeamId).ToImmutableList();
        }

        [JsonIgnore]
        public HearthstonePlayer ActivePlayer
        {
            get => ActivePlayers.First();
            set
            {
                activeTeamId = value.TeamId;
            }
        }

        [JsonIgnore]
        public IEnumerable<IPlayer> NonActivePlayers
        {
            get
            {
                return Players.Where(p => p.TeamId != activeTeamId).ToImmutableList();
            }
        }
    }
}
