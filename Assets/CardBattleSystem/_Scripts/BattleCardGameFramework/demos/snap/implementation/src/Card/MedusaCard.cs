using csbcgf;

namespace snap
{
    public class MedusaCard : SnapCard
    {
        public MedusaCard() : base(2, 2)
        {
            AddComponent(new OnRevealComponent((game, card) => {
                // If played in the middle location (index 1)
                if (card.Owner != null)
                {
                    for (int i = 0; i < SnapConstants.NumberOfLocations; i++)
                    {
                        if (card.Owner.GetCardCollection(SnapConstants.Board + i).Contains(card))
                        {
                            if (i == 1) // Middle location
                            {
                                game.Execute(new ModifyPowerAction(card, 2));
                            }
                            break;
                        }
                    }
                }
            }));
        }
    }
}