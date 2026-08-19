using csbcgf;

namespace mahjong
{
    public class MahjongPlayer : Player
    {
        protected MahjongPlayer() { }

        public MahjongPlayer(bool _ = true) : base(_)
        {
            AddCardCollection(CollectionKeys.Hand, new CardCollection());
            AddCardCollection(CollectionKeys.River, new CardCollection());
        }
    }
}
