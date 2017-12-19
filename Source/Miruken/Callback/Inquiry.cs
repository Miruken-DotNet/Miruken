namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;

    public class Inquiry : ICallback, IAsyncCallback, IDispatchCallback
    {
        private readonly List<object> _resolutions;
        private object _result;

        public Inquiry(object key, bool many = false)
        {
            Key          = key ?? throw new ArgumentNullException(nameof(key));
            Many         = many;
            _resolutions = new List<object>();
        }

        public object Key        { get; }
        public bool   Many       { get; }
        public bool   WantsAsync { get; set; }
        public bool   IsAsync    { get; private set; }

        public CallbackPolicy Policy => ProvidesAttribute.Policy;

        public ICollection<object> Resolutions => _resolutions.AsReadOnly();

        public Type ResultType => WantsAsync || IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result != null) return _result;
                if (!Many)
                {
                    if (_resolutions.Count > 0)
                    {
                        var result = _resolutions[0];
                        _result = (result as Promise)?.Then(
                            (r, s) => r is object[] array ? array.FirstOrDefault() : r) 
                            ?? result;
                    }
                }
                else if (IsAsync)
                {
                    _result = Promise.All(_resolutions
                        .Select(Promise.Resolved).ToArray())
                        .Then((results, s) => Flatten(results)
                        .ToArray());
                }
                else
                {
                    _result = Flatten(_resolutions).ToArray();
                }
                if (WantsAsync && !IsAsync)
                    _result = Promise.Resolved(_result);
                return _result;
            }
            set
            {
                _result = value;
                IsAsync = _result is Promise || _result is Task;
            }
        }

        public bool Resolve(object resolution, IHandler composer)
        {
            return Resolve(resolution, false, false, composer);
        }

        public bool Resolve(object resolution, bool strict,
            bool greedy, IHandler composer)
        {
            if (resolution == null) return false;
            var array    = strict ? null : resolution as object[];
            var resolved = array?.Aggregate(false, 
                (s, res) => Include(res, greedy, composer) || s) 
                         ?? Include(resolution, greedy, composer);
            if (resolved) _result = null;
            return resolved;
        }

        private bool Include(object resolution, bool greedy, IHandler composer)
        {
            if (resolution == null || (!Many && _resolutions.Count > 0))
                return false;

            var promise = resolution as Promise
                       ?? (resolution as Task)?.ToPromise();

            if (promise != null)
            {
                IsAsync = true;
                if (Many) promise = promise.Catch((ex,s) => (object)null);
                promise = promise.Then((result, s) => 
                    result != null && IsSatisfied(result, greedy, composer)
                    ? result : null);
                resolution = promise;
            }
            else if (!IsSatisfied(resolution, greedy, composer))
                return false;

            _resolutions.Add(resolution);
            return true;
        }

        protected virtual bool IsSatisfied(
            object resolution, bool greedy, IHandler composer)
        {
            return true;
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            var isGreedy = greedy;
            var handled  = Implied(handler, false, isGreedy, composer);
            if (handled && !greedy) return true;

            var count = _resolutions.Count;
            handled = Policy.Dispatch(handler, this, greedy, composer, 
                (r,strict) => Resolve(r, strict, isGreedy, composer)) || handled;
            return handled || (_resolutions.Count > count);
        }

        private bool Implied(object item, bool invariant, bool greedy, IHandler composer)
        {
            var type = Key as Type;
            if (type == null) return false;

            var compatible = invariant
                           ? type == item.GetType()
                           : type.IsInstanceOfType(item);

            return compatible && Resolve(item, false, greedy, composer);
        }

        private static IEnumerable<object> Flatten(IEnumerable<object> collection)
        {
            return collection
                .Where(item => item != null)
                .SelectMany(item => item as object[] ?? new[] {item})
                .Distinct();
        }
    }
}
