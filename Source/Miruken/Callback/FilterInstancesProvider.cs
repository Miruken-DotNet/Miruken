﻿namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using Policy;

    public class FilterInstancesProvider : IFilterProvider
    {
        private readonly IFilter[] _filters;

        public FilterInstancesProvider(params IFilter[] filters)
        {
            _filters = filters;
            Required = filters.Length > 0;
        }

        public bool Required { get; }

        public IEnumerable<IFilter> GetFilters(MethodBinding binding,
            Type callbackType, Type logicalResultType, IHandler composer)
        {
            return _filters;
        }
    }
}