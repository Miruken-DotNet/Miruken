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
        Task<TRes> Next(TCb callback, MethodBinding method,
                  IHandler composer, Next<TRes> next,
                  IFilterProvider provider = null);
    }

    public interface IGlobalFilter<in TCb, TRes> : IFilter<TCb, TRes> { }

    public interface IFilterProvider
    {
        IEnumerable<IFilter> GetFilters(MethodBinding binding,
            Type callbackType, Type logicalResultType, IHandler composer);
    }

    public class FilterInstancesProvider : IFilterProvider
    {
        private readonly IFilter[] _filters;

        public FilterInstancesProvider(params IFilter[] filters)
        {
            _filters = filters;
        }

        public IEnumerable<IFilter> GetFilters(MethodBinding binding,
            Type callbackType, Type logicalResultType, IHandler composer)
        {
            return _filters;
        }
    }
}
