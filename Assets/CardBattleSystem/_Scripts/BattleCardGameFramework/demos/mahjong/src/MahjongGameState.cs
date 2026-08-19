using csbcgf;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace mahjong
{
    public class MahjongGameState : GameState
    {
        [JsonProperty]
        protected int activePlayerIndex;

        [JsonProperty]
        protected IDictionary<string, ICardCollection> cardCollections = null!;

        protected MahjongGameState() { }

        public MahjongGameState(bool _ = true) : base(_)
        {
            this.cardCollections = new Dictionary<string, ICardCollection>();
            AddCardCollection(CollectionKeys.Wall, new CardCollection());
            activePlayerIndex = 0;
        }

        public ICardCollection GetCardCollection(string key)
        {
            return cardCollections[key];
        }

        public void AddCardCollection(string key, ICardCollection cardCollection)
        {
            cardCollections.Add(key, cardCollection);
        }

        [JsonIgnore]
        public int ActivePlayerIndex
        {
            get => activePlayerIndex;
            set => activePlayerIndex = value;
        }

        [JsonIgnore]
        public MahjongPlayer ActivePlayer
        {
            get => (MahjongPlayer)Players.ElementAt(activePlayerIndex);
            set => activePlayerIndex = Players.ToList().IndexOf(value);
        }
    }
}
