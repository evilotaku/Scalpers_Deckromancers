using csbcgf;

namespace mahjong
{
    public class MahjongTileComponent : CardComponent
    {
        protected MahjongTileComponent() { }

        public MahjongTileComponent(MahjongSuit suit, int value)
            : base(true)
        {
            AddStat(StatKeys.Suit, new Stat((int)suit, (int)suit));
            AddStat(StatKeys.Value, new Stat(value, value));
        }
    }
}
