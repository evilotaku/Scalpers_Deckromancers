using csbcgf;

namespace snap
{
    public class SnapCardComponent : CardComponent
    {
        protected SnapCardComponent() { }

        public SnapCardComponent(int cost, int power) : base(true)
        {
            AddStat(SnapConstants.Cost, new Stat(cost, cost));
            AddStat(SnapConstants.Power, new Stat(power, power));
        }
    }
}