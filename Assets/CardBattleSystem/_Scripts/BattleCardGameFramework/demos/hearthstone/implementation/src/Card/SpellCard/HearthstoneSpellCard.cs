using csbcgf;

namespace hearthstone
{
    public class HearthstoneSpellCard : Card
    {
        protected HearthstoneSpellCard()
        {
        }

        public HearthstoneSpellCard(bool _ = true) : base(_)
        {
        }

        public virtual bool IsCastable(IGameState gameState)
        {
            HearthstoneGameState state = (HearthstoneGameState)gameState;
            return Owner != null
                && Owner.GetCardCollection(CardCollectionKeys.Hand).Contains(this)
                && Owner.TeamId == state.ActiveTeamId
                && GetValue(StatKeys.Mana) <= Owner.GetValue(StatKeys.Mana);
        }
    }
}
