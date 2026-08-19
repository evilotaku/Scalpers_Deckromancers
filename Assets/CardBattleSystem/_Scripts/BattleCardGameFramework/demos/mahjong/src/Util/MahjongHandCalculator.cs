using csbcgf;
using System.Collections.Generic;
using System.Linq;

namespace mahjong
{
    public static class MahjongHandCalculator
    {
        public static bool IsWinningHand(IEnumerable<ICard> hand)
        {
            if (hand == null) return false;
            
            // Group tiles by suit
            var grouped = hand.GroupBy(c => (MahjongSuit)c.GetValue(StatKeys.Suit))
                              .ToDictionary(g => g.Key, g => g.Select(c => c.GetValue(StatKeys.Value)).OrderBy(v => v).ToList());

            // Check if hand size is 3n + 2
            if (hand.Count() % 3 != 2) return false;

            // Try placing the pair in each suit that has at least 2 tiles
            foreach (var suit in grouped.Keys)
            {
                var values = grouped[suit];
                var uniqueValues = values.Distinct();
                foreach (var v in uniqueValues)
                {
                    if (values.Count(x => x == v) >= 2)
                    {
                        // Found a potential pair in this suit
                        var remainingInSuit = new List<int>(values);
                        remainingInSuit.Remove(v);
                        remainingInSuit.Remove(v);

                        bool allSuitsValid = true;
                        
                        // Check if this suit's remainder can form melds
                        if (!CanFormMelds(remainingInSuit, suit))
                        {
                            allSuitsValid = false;
                        }
                        else
                        {
                            // Check all other suits
                            foreach (var otherSuit in grouped.Keys)
                            {
                                if (otherSuit == suit) continue;
                                if (!CanFormMelds(grouped[otherSuit], otherSuit))
                                {
                                    allSuitsValid = false;
                                    break;
                                }
                            }
                        }

                        if (allSuitsValid) return true;
                    }
                }
            }
            return false;
        }

        private static bool CanFormMelds(List<int> values, MahjongSuit suit)
        {
            if (values.Count == 0) return true;
            if (values.Count % 3 != 0) return false;

            int first = values[0];
            
            // Try Pung (3 of a kind) - Valid for all suits
            if (values.Count(x => x == first) >= 3)
            {
                List<int> remaining = new List<int>(values);
                remaining.Remove(first);
                remaining.Remove(first);
                remaining.Remove(first);
                if (CanFormMelds(remaining, suit)) return true;
            }
            
            // Try Chow (3 in a row) - ONLY valid for Dots, Bamboos, and Characters
            if (suit != MahjongSuit.Winds && suit != MahjongSuit.Dragons)
            {
                if (values.Contains(first + 1) && values.Contains(first + 2))
                {
                    List<int> remaining = new List<int>(values);
                    remaining.Remove(first);
                    remaining.Remove(first + 1);
                    remaining.Remove(first + 2);
                    if (CanFormMelds(remaining, suit)) return true;
                }
            }
            
            return false;
        }
    }
}
