using csbcgf;
using System;

namespace snap
{
    public class OnRevealComponent : CardComponent, IReaction
    {
        protected Action<IGame, SnapCard> effect;

        public OnRevealComponent(Action<IGame, SnapCard> effect) : base(true)
        {
            this.effect = effect;
            AddReaction(this);
        }

        public void ReactBefore(IGame game, IAction action)
        {
        }

        public void ReactAfter(IGame game, IAction action)
        {
            if (action is RevealCardAction revealAction && revealAction.Card == ParentCard)
            {
                effect?.Invoke(game, (SnapCard)ParentCard);
            }
        }
    }
}