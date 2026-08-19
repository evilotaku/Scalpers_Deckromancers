namespace snap
{
    public static class SnapConstants
    {
        // Stat Keys
        public const string Cost = "Cost";
        public const string Power = "Power";
        public const string Energy = "Energy";
        public const string MaxEnergy = "MaxEnergy";
        public const string Turn = "Turn";

        // Collection Keys
        public const string Hand = "Hand";
        public const string Deck = "Deck";
        public const string Board = "Board"; // Base name, will be combined with location index
        public const string Pending = "Pending"; // Cards played this turn but not yet revealed

        // Game Settings
        public const int MaxTurns = 6;
        public const int MaxHandSize = 7;
        public const int MaxBoardSizePerLocation = 4;
        public const int NumberOfLocations = 3;
    }
}