using csbcgf;
using Newtonsoft.Json;

namespace snap
{
    public class ModifyEnergyAction : Action
    {
        [JsonProperty]
        protected IStatContainer player = null!;

        [JsonProperty]
        protected int deltaValue;

        [JsonProperty]
        protected int deltaBaseValue;

        protected ModifyEnergyAction() { }

        public ModifyEnergyAction(IStatContainer player, int deltaValue, int deltaBaseValue = 0, bool isAborted = false)
            : base(isAborted)
        {
            this.player = player;
            this.deltaValue = deltaValue;
            this.deltaBaseValue = deltaBaseValue;
        }

        public override void Execute(IGame game)
        {
            player.AddStat(SnapConstants.Energy, new Stat(deltaValue, deltaBaseValue));
        }

        public override bool IsExecutable(IGameState gameState)
        {
            return true;
        }
    }
}