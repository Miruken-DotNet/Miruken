namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using Policy;

    public delegate Res Next<out Res>(
        bool proceed = true, IHandler composer = null);

    public interface IFilter
    {
        int? Order { get; set; }
    }

    public interface IFilter<in Cb, Res> : IFilter
    {
        Res Next(Cb callback, MethodBinding method,
                 IHandler composer, Next<Res> next);
    }

    public interface IGlobalFilter<in Cb, Res> : IFilter<Cb, Res> { }

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

    public class FilterComparer : IComparer<IFilter>
    {
        public static readonly FilterComparer Instance = new FilterComparer();

        public int Compare(IFilter x, IFilter y)
        {
            if (x == y) return 0;
            if (y?.Order == null) return -1;
            if (x?.Order == null) return 1;
            return x.Order.Value - y.Order.Value;
        }
    }
}
