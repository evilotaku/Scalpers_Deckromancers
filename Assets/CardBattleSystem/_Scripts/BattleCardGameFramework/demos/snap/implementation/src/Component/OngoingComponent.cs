using csbcgf;

namespace snap
{
    public abstract class OngoingComponent : CardComponent, IReaction
    {
        protected OngoingComponent() : base(true) 
        {
            AddReaction(this);
        }

        public void ReactBefore(IGame game, IAction action)
        {
        }

        public void ReactAfter(IGame game, IAction action)
        {
            if (ParentCard is SnapCard card && card.IsRevealed)
            {
                UpdateEffect(game);
            }
        }

        protected abstract void UpdateEffect(IGame game);
    }
}