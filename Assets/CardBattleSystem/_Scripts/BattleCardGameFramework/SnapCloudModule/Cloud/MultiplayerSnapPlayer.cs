using Newtonsoft.Json;

namespace snap
{
    public class MultiplayerSnapPlayer : SnapPlayer
    {
        [JsonProperty]
        public string PlayerId { get; set; }

        protected MultiplayerSnapPlayer() { }

        public MultiplayerSnapPlayer(string playerId, int teamId) : base(teamId)
        {
            PlayerId = playerId;
        }
    }
}