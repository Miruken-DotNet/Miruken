namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;

    public delegate bool ResultsDelegate(object result, bool strict);

    public abstract class MethodBinding
    {
        private IFilterProvider[] _filters;

        protected MethodBinding(MethodDispatch dispatch)
        {
            Dispatcher = dispatch 
                      ?? throw new ArgumentNullException(nameof(dispatch));
            _filters = dispatch.Attributes.OfType<IFilterProvider>()
                .ToArray().Normalize();
        }

        public MethodDispatch Dispatcher { get; }

        public IEnumerable<IFilterProvider> Filters => _filters;

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            _filters = _filters.Concat(providers.Where(p => p != null))
                .ToArray().Normalize();
        }

        public void AddFilters(params Type[] filterTypes)
        {
            if (filterTypes == null || filterTypes.Length == 0) return;
            AddFilters(new FilterAttribute(filterTypes));
        }

        public abstract bool Dispatch(object target, object callback,
            IHandler composer, ResultsDelegate results = null);

        internal object CoerceResult(
            object result, Type resultType, bool? wantsAsync = null)
        {
            if (wantsAsync == true)
            {
                var promise = result as Promise;
                return promise ?? Promise.Resolved(result);
            }
            if (result == null || !resultType.IsInstanceOfType(result))
            {
                if (resultType.Is<Task>())
                    return Promise.Resolved(result).ToTask().Coerce(resultType);
                if (resultType.Is<Promise>())
                    return Promise.Resolved(result).Coerce(resultType);
                var promise = result as Promise;
                if (promise == null)
                    if (result is Task task) promise = Promise.Resolved(task);
                if (promise != null)
                    return promise.Wait();
            }
            return result;
        }
    }
}
