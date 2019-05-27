namespace Miruken
{
    public interface IIdentifiable<TId>
    {
        TId Id { get; set; }
    }

    public interface IIdentifiable : IIdentifiable<int>
    {
    }
}
