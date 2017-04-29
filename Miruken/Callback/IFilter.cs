namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using Policy;

    public delegate Res FilterDelegate<out Res>(
        bool proceed = true, IHandler composer = null);

    public interface IFilter
    {
        int? Order { get; set; }
    }

    public interface IFilter<in Cb, Res> : IFilter
    {
        Res Filter(Cb callback, MethodBinding method,
                   IHandler composer, FilterDelegate<Res> proceed);
    }

    public interface IDynamicFilter : IFilter
    {
        object Filter(object callback, MethodBinding method,
           IHandler composer, FilterDelegate<object> proceed);
    }

    public interface IFilterProvider
    {
        IEnumerable<IFilter> GetFilters(
            Type callbackType, Type resulType, IHandler composer);
    }

    public class FilterInstancesProvider : IFilterProvider
    {
        private readonly IFilter[] _filters;

        public FilterInstancesProvider(params IFilter[] filters)
        {
            _filters = filters;
        }

        public IEnumerable<IFilter> GetFilters(
            Type callbackType, Type resulType, IHandler composer)
        {
            return _filters;
        }
    }
}
