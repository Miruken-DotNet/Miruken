namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;

    public abstract class MethodBinding
    {
        private List<IFilterProvider> _filters;

        protected MethodBinding(MethodDispatch dispatch)
        {
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            Dispatcher = dispatch;
            AddFilters(FilterAttribute.GetFilters(Dispatcher.Method));
        }

        public MethodDispatch Dispatcher { get; }

        public IEnumerable<IFilterProvider> Filters =>
            _filters ?? Enumerable.Empty<IFilterProvider>();

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            if (_filters == null)
                _filters = new List<IFilterProvider>();
            _filters.AddRange(providers.Where(p => p != null));
        }

        public abstract bool Dispatch(object target, object callback,
            IHandler composer, Func<object, bool> results = null);

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
                if (typeof(Task).IsAssignableFrom(resultType))
                    return Promise.Resolved(result).ToTask().Coerce(resultType);
                if (typeof(Promise).IsAssignableFrom(resultType))
                    return Promise.Resolved(result).Coerce(resultType);
                var promise = result as Promise;
                if (promise == null)
                {
                    var task = result as Task;
                    if (task != null) promise = Promise.Resolved(task);
                }
                if (promise != null) return promise.Wait();
            }
            return result;
        }
    }
}
