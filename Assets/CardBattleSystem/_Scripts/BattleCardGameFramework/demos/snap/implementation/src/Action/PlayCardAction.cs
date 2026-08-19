using csbcgf;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace snap
{
    public class PlayCardAction : csbcgf.Action<SnapGameState>
    {
        [JsonProperty]
        protected SnapPlayer player = null!;

        [JsonProperty]
        protected SnapCard card = null!;

        [JsonProperty]
        protected int locationIndex;

        protected PlayCardAction() { }

        public PlayCardAction(SnapPlayer player, SnapCard card, int locationIndex, bool isAborted = false)
            : base(isAborted)
        {
            this.player = player;
            this.card = card;
            this.locationIndex = locationIndex;
        }

        public override void Execute(IGame<SnapGameState> game)
        {
            int cost = card.GetValue(SnapConstants.Cost);
            string pendingKey = SnapConstants.Pending + locationIndex;

            game.ExecuteSequentially(new List<IAction> {
                new ModifyEnergyAction(player, -cost),
                new RemoveCardFromCardCollectionAction(player.GetCardCollection(SnapConstants.Hand), card),
                new AddCardToCardCollectionAction(player.GetCardCollection(pendingKey), card)
            });
        }

        public override bool IsExecutable(SnapGameState gameState)
        {
            return card.IsPlayable(gameState, locationIndex);
        }
    }
}