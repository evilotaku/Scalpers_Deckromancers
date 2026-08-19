using csbcgf;
using Newtonsoft.Json;

namespace snap
{
    public class SnapCard : Card
    {
        [JsonProperty]
        protected bool isRevealed;

        protected SnapCard() { }

        public SnapCard(int cost, int power) : base(true)
        {
            this.isRevealed = false;
            AddComponent(new SnapCardComponent(cost, power));
        }

        [JsonIgnore]
        public bool IsRevealed
        {
            get => isRevealed;
            set => isRevealed = value;
        }

        public virtual bool IsPlayable(SnapGameState gameState, int locationIndex)
        {
            if (Owner == null) return false;
            
            SnapPlayer player = (SnapPlayer)Owner;
            if (!player.GetCardCollection(SnapConstants.Hand).Contains(this)) return false;
            
            // Check Energy
            if (GetValue(SnapConstants.Cost) > player.GetValue(SnapConstants.Energy)) return false;
            
            // Check Location capacity
            string boardKey = SnapConstants.Board + locationIndex;
            if (player.GetCardCollection(boardKey).IsFull) return false;

            return true;
        }
    }
}