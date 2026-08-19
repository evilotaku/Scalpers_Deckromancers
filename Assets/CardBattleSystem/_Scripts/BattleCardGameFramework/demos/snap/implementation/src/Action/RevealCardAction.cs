using csbcgf;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace snap
{
    public class RevealCardAction : csbcgf.Action<SnapGameState>
    {
        [JsonProperty]
        protected SnapCard card = null!;

        [JsonProperty]
        protected int locationIndex;

        protected RevealCardAction() { }

        public RevealCardAction(SnapCard card, int locationIndex, bool isAborted = false)
            : base(isAborted)
        {
            this.card = card;
            this.locationIndex = locationIndex;
        }

        [JsonIgnore]
        public SnapCard Card => card;

        public override void Execute(IGame<SnapGameState> game)
        {
            card.IsRevealed = true;
            string pendingKey = SnapConstants.Pending + locationIndex;
            string boardKey = SnapConstants.Board + locationIndex;

            game.ExecuteSequentially(new List<IAction> {
                new RemoveCardFromCardCollectionAction(card.Owner.GetCardCollection(pendingKey), card),
                new AddCardToCardCollectionAction(card.Owner.GetCardCollection(boardKey), card)
            });
        }

        public override bool IsExecutable(SnapGameState gameState)
        {
            return !card.IsRevealed;
        }
    }
}