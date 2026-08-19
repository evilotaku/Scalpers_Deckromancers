using csbcgf;
using Newtonsoft.Json;

namespace mahjong
{
    public class MahjongDiscardTileAction : csbcgf.Action
    {
        [JsonProperty]
        protected ICard tile = null!;

        protected MahjongDiscardTileAction() { }

        public MahjongDiscardTileAction(ICard tile, bool isAborted = false) : base(isAborted)
        {
            this.tile = tile;
        }

        [JsonIgnore]
        public ICard Tile => tile;

        public override void Execute(IGame game)
        {
            MahjongGameState gameState = (MahjongGameState)game.State;
            MahjongPlayer activePlayer = gameState.ActivePlayer;
            
            ICardCollection hand = activePlayer.GetCardCollection(CollectionKeys.Hand);
            ICardCollection river = activePlayer.GetCardCollection(CollectionKeys.River);
            
            if (hand.Contains(Tile))
            {
                game.Execute(new RemoveCardFromCardCollectionAction(hand, Tile));
                game.Execute(new AddCardToCardCollectionAction(river, Tile));
            }
        }

        public override bool IsExecutable(IGameState gameState)
        {
            if (!(gameState is MahjongGameState mahjongGameState)) return false;
            MahjongPlayer activePlayer = mahjongGameState.ActivePlayer;
            ICardCollection hand = activePlayer.GetCardCollection(CollectionKeys.Hand);
            
            return Tile != null && hand.Contains(Tile);
        }
    }
}
