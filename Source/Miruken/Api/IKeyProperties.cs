namespace Miruken.Api
{
    public interface IKeyProperties<TId>
    {
        TId Id      { get; set; }
        string Name { get; set; }
    }
}
