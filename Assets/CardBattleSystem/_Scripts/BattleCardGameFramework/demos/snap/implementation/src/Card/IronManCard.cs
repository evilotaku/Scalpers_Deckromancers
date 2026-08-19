using csbcgf;
using System.Linq;

namespace snap
{
    public class IronManCard : SnapCard
    {
        public IronManCard() : base(5, 0)
        {
            AddComponent(new IronManOngoingComponent());
        }
    }

    public class IronManOngoingComponent : OngoingComponent
    {
        public IronManOngoingComponent() : base() { }

        protected override void UpdateEffect(IGame game)
        {
            // Iron Man doubles the total power at this location.
            // This is actually tricky because 'Total Power' is calculated at the location level.
            // In Snap, Ongoing effects like Iron Man apply a multiplier to the final sum.
            
            // For this demo, we'll let SnapLocation handle the Iron Man check.
        }
    }
}