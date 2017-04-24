namespace Miruken.Callback
{
    using System.Collections.Generic;

    public delegate Res FilterDelegate<out Res>(bool proceed = true);

    public interface IFilter
    {
        int? Order { get; set; }
    }

    public interface IFilter<in Cb, Res> : IFilter
    {
        Res Filter(Cb callback, IHandler composer, FilterDelegate<Res> proceed);
    }

    public interface IFilterProvider
    {
        IEnumerable<IFilter> GetFilters(IHandler composer);
    }

}
