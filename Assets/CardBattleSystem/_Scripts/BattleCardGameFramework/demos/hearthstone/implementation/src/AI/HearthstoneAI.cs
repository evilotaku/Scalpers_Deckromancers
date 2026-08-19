using System.Collections.Generic;
using System.Linq;
using csbcgf;

namespace hearthstone
{
    public class HearthstoneAI
    {
        public void Action(HearthstoneGame game)
        {
            HearthstoneGameState state = game.State;
            foreach (HearthstonePlayer player in state.ActivePlayers)
            {
                if (!player.IsAI) continue;

                // 1. Try to summon monsters
                ICardCollection hand = player.GetCardCollection(CardCollectionKeys.Hand);
                List<HearthstoneMonsterCard> monstersInHand = hand.Cards
                    .OfType<HearthstoneMonsterCard>()
                    .ToList();

                foreach (var monster in monstersInHand)
                {
                    if (monster.IsSummonable(state))
                    {
                        player.SummonMonster(game, monster);
                    }
                }

                // 2. Try to attack with monsters on board
                ICardCollection board = player.GetCardCollection(CardCollectionKeys.Board);
                List<HearthstoneMonsterCard> monstersOnBoard = board.Cards
                    .OfType<HearthstoneMonsterCard>()
                    .Where(c => c.IsReadyToAttack)
                    .ToList();

                foreach (var monster in monstersOnBoard)
                {
                    var targets = monster.GetPotentialTargets(state);
                    if (targets.Any())
                    {
                        // Prioritize monsters, then players
                        IStatContainer target = (IStatContainer)targets.OfType<HearthstoneMonsterCard>().FirstOrDefault() 
                                                ?? (IStatContainer)targets.OfType<HearthstonePlayer>().FirstOrDefault();
                        
                        if (target != null)
                        {
                            monster.Attack(game, target);
                        }
                    }
                }
            }

            // End turn after actions
            game.NextTurn();
        }
    }
}
