using mahjong;
using Newtonsoft.Json;

namespace csbcgf
{
    public class MultiplayerMahjongPlayer : MahjongPlayer
    {
        [JsonProperty]
        public string PlayerId { get; set; } = string.Empty;

        protected MultiplayerMahjongPlayer() { }

        public MultiplayerMahjongPlayer(string playerId) : base(true)
        {
            this.PlayerId = playerId;
        }
    }
}
