namespace csbcgf
{
    public interface ICard : IStatContainer, IReactive, ICompound, IOwnable
    {
        int Id { get; }
    }
}
