using csbcgf;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace snap
{
    public class DrawCardAction : csbcgf.Action<SnapGameState>
    {
        [JsonProperty]
        protected IPlayer player = null!;

        [JsonProperty]
        protected ICard drawnCard = null!;

        protected DrawCardAction() { }

        public DrawCardAction(IPlayer player, bool isAborted = false)
            : base(isAborted)
        {
            this.player = player;
        }

        public override void Execute(IGame<SnapGameState> game)
        {
            drawnCard = player.GetCardCollection(SnapConstants.Deck).Last;
            game.ExecuteSequentially(new List<IAction> {
                new RemoveCardFromCardCollectionAction(player.GetCardCollection(SnapConstants.Deck), drawnCard),
                new AddCardToCardCollectionAction(player.GetCardCollection(SnapConstants.Hand), drawnCard)
            });
        }

        public override bool IsExecutable(SnapGameState gameState)
        {
            return !player.GetCardCollection(SnapConstants.Deck).IsEmpty 
                && !player.GetCardCollection(SnapConstants.Hand).IsFull;
        }
    }
}