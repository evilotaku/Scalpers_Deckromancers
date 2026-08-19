using System.Collections.Generic;

namespace csbcgf
{
    public interface IPlayer : IStatContainer, IReactive
    {
        /// <summary>
        /// Get all Cards from the Player's Decks.
        /// </summary>
        IEnumerable<ICard> AllCards { get; }

        /// <summary>
        /// The team this player belongs to.
        /// </summary>
        int TeamId { get; set; }

        /// <summary>
        /// Whether this player is controlled by AI.
        /// </summary>
        bool IsAI { get; set; }

        ICardCollection GetCardCollection(string key);

        void AddCardCollection(string key, ICardCollection cardCollection);

        bool RemoveCardCollection(string key);
    }
}
