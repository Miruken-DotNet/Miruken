﻿namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Policy;
    using Policy.Bindings;

    public delegate Task<TRes> Next<TRes>(
        IHandler composer = null, bool proceed = true);

    public interface IFilter : IOrdered { }

    public interface IFilter<in TCb, TRes> : IFilter
    {
        Task<TRes> Next(TCb callback,
            object rawCallback, MemberBinding member,
            IHandler composer, Next<TRes> next,
            IFilterProvider provider = null);
    }

    public interface IFilterProvider
    {
        bool Required { get; }

        bool? AppliesTo(object callback, Type callbackType);

        IEnumerable<IFilter> GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            object callback, Type callbackType, IHandler composer);
    }

    public interface IFiltered
    {
        IEnumerable<IFilterProvider> Filters { get; }
        void AddFilters(params IFilterProvider[] providers);
        void RemoveFilters(params IFilterProvider[] providers);
        void RemoveAllFilters();
    }
}
