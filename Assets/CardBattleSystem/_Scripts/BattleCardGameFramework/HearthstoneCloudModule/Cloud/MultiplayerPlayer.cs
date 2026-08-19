using Newtonsoft.Json;
using hearthstone;

namespace csbcgf
{
    public class MultiplayerPlayer : HearthstonePlayer
    {
        [JsonProperty]
        public string PlayerId { get; set; } = string.Empty;

        protected MultiplayerPlayer() { }

        public MultiplayerPlayer(string playerId) : base(true)
        {
            this.PlayerId = playerId;
        }
    }
}
