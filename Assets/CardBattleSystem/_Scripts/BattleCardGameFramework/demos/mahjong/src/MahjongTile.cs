using csbcgf;

namespace mahjong
{
    public class MahjongTile : Card
    {
        protected MahjongTile() { }

        public MahjongTile(MahjongSuit suit, int value) : base(true)
        {
            AddComponent(new MahjongTileComponent(suit, value));
        }
    }
}
