namespace mahjong
{
    public enum MahjongSuit
    {
        Dots = 0,
        Bamboos = 1,
        Characters = 2,
        Winds = 3,
        Dragons = 4
    }

    public class StatKeys
    {
        public const string Suit = "Suit";
        public const string Value = "Value";
    }

    public class CollectionKeys
    {
        public const string Wall = "Wall";
        public const string Hand = "Hand";
        public const string River = "River";
    }
}
