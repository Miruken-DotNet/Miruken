namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Policy;

    public delegate Task<Res> Next<Res>(
        IHandler composer = null, bool proceed = true);

    public interface IFilter
    {
        int? Order { get; set; }
    }

    public interface IFilter<in TCb, TRes> : IFilter
    {
        Task<TRes> Next(
            TCb callback, MethodBinding method,
            IHandler composer, Next<TRes> next,
            IFilterProvider provider = null);
    }

    public interface IFilterProvider
    {
        bool Required { get;}

        IEnumerable<IFilter> GetFilters(MethodBinding binding,
            Type callbackType, Type logicalResultType, IHandler composer);
    }

    public interface IFiltered
    {
        IEnumerable<IFilterProvider> Filters { get; }
        void AddFilters(params IFilterProvider[] providers);
        void AddFilters(params Type[] filterTypes);
    }
}
