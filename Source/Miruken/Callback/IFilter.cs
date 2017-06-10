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

    public interface IFilterProvider
    {
        IEnumerable<IFilter> GetFilters(
            Type callbackType, Type logicalResultType, IHandler composer);
    }

    public class FilterInstancesProvider : IFilterProvider
    {
        private readonly IFilter[] _filters;

        public FilterInstancesProvider(params IFilter[] filters)
        {
            _filters = filters;
        }

        public IEnumerable<IFilter> GetFilters(
            Type callbackType, Type logicalResultType, IHandler composer)
        {
            return _filters;
        }
    }
}
