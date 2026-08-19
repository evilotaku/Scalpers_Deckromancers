using csbcgf;
using System.Linq;

namespace snap
{
    public class SnapLocation
    {
        public int Index { get; }
        public string Name { get; }

        public SnapLocation(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public int GetPower(SnapGameState state, int teamId)
        {
            SnapPlayer player = (SnapPlayer)state.Players.First(p => p.TeamId == teamId);
            string boardKey = SnapConstants.Board + Index;
            var cards = player.GetCardCollection(boardKey).Cards
                .Cast<SnapCard>()
                .Where(c => c.IsRevealed)
                .ToList();

            int basePower = cards.Sum(c => c.GetValue(SnapConstants.Power));
            
            // Apply Iron Man multiplier
            int ironManCount = cards.Count(c => c is IronManCard);
            if (ironManCount > 0)
            {
                basePower *= (int)System.Math.Pow(2, ironManCount);
            }

            return basePower;
        }
    }
}