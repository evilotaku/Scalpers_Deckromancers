using csbcgf;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace hearthstone
{
    public class TestSpell : HearthstoneTargetlessSpellCard
    {
        protected TestSpell() { }

        public TestSpell(bool _ = true) : base(_)
        {
            AddComponent(new TestSpellComponent());
        }

        public class TestSpellComponent : HearthstoneTargetlessSpellCardComponent
        {
            protected TestSpellComponent() { }

            public TestSpellComponent(bool _ = true) : base(2)
            {
            }

            public override void Cast(HearthstoneGame game)
            {
                for (int i = 0; i < 2; i++) game.Execute(new DrawCardAction(game.State.ActivePlayer));
            }
        }
    }
}
