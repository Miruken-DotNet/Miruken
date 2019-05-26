namespace Miruken.Api
{
    using System;

    public interface IIdentifiable : IIdentifiable<int>
    {
    }

    public interface IIdentifiable<TId>
    {
        TId Id { get; set; }
    }

    public interface IKeyProperties<TId> : IIdentifiable<TId>
    {
        string Name { get; set; }
    }

    public interface IVersioned
    {
        byte[]   RowVersion { get; set; }
        DateTime Modified   { get; set; }
    }
}
