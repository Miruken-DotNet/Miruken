namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using Policy;

    public delegate Res NextDelegate<out Res>(
        bool proceed = true, IHandler composer = null);

    public interface IFilter
    {
        int? Order { get; set; }
    }

    public interface IFilter<in Cb, Res> : IFilter
    {
        Res Next(Cb callback, MethodBinding method,
                 IHandler composer, NextDelegate<Res> next);
    }

    public interface IDynamicFilter : IFilter
    {
        object Next(object callback, MethodBinding method,
           IHandler composer, NextDelegate<object> next);
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
