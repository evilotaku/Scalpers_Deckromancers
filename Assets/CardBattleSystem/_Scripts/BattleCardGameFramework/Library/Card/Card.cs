using Newtonsoft.Json;

namespace csbcgf
{
    public abstract class Card : ReactiveCompound, ICard
    {
        [JsonProperty]
        protected int id;

        [JsonProperty]
        protected IPlayer owner;

        protected Card() { }

        public Card(bool _ = true) : base(_)
        {
        }

        [JsonIgnore]
        public int Id 
        { 
            get => id;
            set => id = value; 
        }

        [JsonIgnore]
        public IPlayer Owner
        {
            get => owner;
            set => owner = value;
        }

        int ICard.Id => Id;

        IPlayer IOwnable.Owner { get => Owner; set => Owner = value; }

        public override void AddComponent(ICardComponent cardComponent)
        {
            base.AddComponent(cardComponent);
            cardComponent.ParentCard = this;
        }
    }
}
