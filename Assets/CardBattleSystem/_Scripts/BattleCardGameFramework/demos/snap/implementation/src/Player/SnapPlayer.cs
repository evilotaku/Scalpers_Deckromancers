using csbcgf;

namespace snap
{
    public class SnapPlayer : Player
    {
        protected SnapPlayer() { }

        public SnapPlayer(int teamId) : base(true)
        {
            this.TeamId = teamId;
            
            AddStat(SnapConstants.Energy, new Stat(0, 0, 0));
            AddStat(SnapConstants.MaxEnergy, new Stat(0, 0, 0));

            AddCardCollection(SnapConstants.Deck, new CardCollection());
            AddCardCollection(SnapConstants.Hand, new CardCollection(SnapConstants.MaxHandSize));
            
            for (int i = 0; i < SnapConstants.NumberOfLocations; i++)
            {
                AddCardCollection(SnapConstants.Board + i, new CardCollection(SnapConstants.MaxBoardSizePerLocation));
                AddCardCollection(SnapConstants.Pending + i, new CardCollection(SnapConstants.MaxBoardSizePerLocation));
            }
        }
    }
}