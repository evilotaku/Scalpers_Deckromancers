using csbcgf;
using Newtonsoft.Json;

namespace snap
{
    public class ModifyPowerAction : Action
    {
        [JsonProperty]
        protected IStatContainer target = null!;

        [JsonProperty]
        protected int delta;

        protected ModifyPowerAction() { }

        public ModifyPowerAction(IStatContainer target, int delta, bool isAborted = false)
            : base(isAborted)
        {
            this.target = target;
            this.delta = delta;
        }

        public override void Execute(IGame game)
        {
            target.AddStat(SnapConstants.Power, new Stat(delta, 0));
        }

        public override bool IsExecutable(IGameState gameState)
        {
            return true;
        }
    }
}