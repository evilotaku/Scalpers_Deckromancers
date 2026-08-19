using csbcgf;
using System.Collections.Generic;

namespace hearthstone
{
    public class TestMonster : HearthstoneMonsterCard
    {
        protected TestMonster() { }

        public TestMonster(bool _ = true) : base(3, 4, 5)
        {
            AddReaction(new DivineShield());
            AddReaction(new TestMonsterBattlecryReaction(this));
        }

        public class TestMonsterBattlecryReaction : CardReaction<HearthstoneGameState, HearthstoneGame, SummonMonsterAction>
        {
            protected TestMonsterBattlecryReaction() { }

            public TestMonsterBattlecryReaction(ICard card) : base(card) { }

            public override void ReactAfter(HearthstoneGame game, SummonMonsterAction action)
            {
                if (action.MonsterCard == ParentCard)
                {
                    game.Execute(new DrawCardAction(game.State.ActivePlayer));
                }
            }
        }
    }
}
