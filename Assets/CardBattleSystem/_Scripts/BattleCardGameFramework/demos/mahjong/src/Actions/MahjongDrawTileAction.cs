using csbcgf;

namespace mahjong
{
    public class MahjongDrawTileAction : csbcgf.Action
    {
        protected MahjongDrawTileAction() { }

        public MahjongDrawTileAction(bool isAborted = false) : base(isAborted)
        {
        }

        public override void Execute(IGame game)
        {
            MahjongGameState gameState = (MahjongGameState)game.State;
            ICardCollection wall = gameState.GetCardCollection(CollectionKeys.Wall);
            
            if (!wall.IsEmpty)
            {
                ICard tile = wall.First;
                game.Execute(new RemoveCardFromCardCollectionAction(wall, tile));
                
                MahjongPlayer activePlayer = gameState.ActivePlayer;
                ICardCollection hand = activePlayer.GetCardCollection(CollectionKeys.Hand);
                game.Execute(new AddCardToCardCollectionAction(hand, tile));
            }
        }

        public override bool IsExecutable(IGameState gameState)
        {
            if (!(gameState is MahjongGameState mahjongGameState)) return false;
            ICardCollection wall = mahjongGameState.GetCardCollection(CollectionKeys.Wall);
            return !wall.IsEmpty;
        }
    }
}
