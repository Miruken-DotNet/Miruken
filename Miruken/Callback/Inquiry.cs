namespace Miruken.Callback
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;

    public class Inquiry : ICallback, ICallbackDispatch
    {
        private readonly List<object> _resolutions;
        private object _result;

        public Inquiry(object key, bool many = false)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Key          = key;
            Many         = many;
            _resolutions = new List<object>();
        }

        public object Key     { get; }
        public bool   Many    { get; }
        public bool   IsAsync { get; private set; }

        public ICollection<object> Resolutions => _resolutions.AsReadOnly();

        public Type ResultType => IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result != null) return _result;
                if (!Many)
                {
                    if (_resolutions.Count > 0)
                    {
                        var result  = _resolutions[0];
                        var promise = result as Promise;
                        _result = promise?.Then((r,s) => 
                            RuntimeHelper.IsCollection(r)
                                ? ((IEnumerable)r).Cast<object>().FirstOrDefault()
                                : r) ?? result;
                    }
                }
                else if (IsAsync)
                {
                    _result = Promise.All(_resolutions
                        .Select(r => (r as Promise) ?? Promise.Resolved(r))
                        .ToArray())
                        .Then((results, s) => Flatten(results));
                }
                else
                {
                    _result = Flatten(_resolutions);
                }
                return _result;
            }
            set { _result = value; }
        }

        public bool Resolve(object resolution, IHandler composer)
        {
            if (resolution == null) return false;
            var resolved = RuntimeHelper.IsCollection(resolution)
                 ? ((IEnumerable)resolution).Cast<object>()
                    .Aggregate(false, (s, res) => Include(res, composer) || s)
                 : Include(resolution, composer);
            if (resolved) _result = null;
            return resolved;
        }

        private bool Include(object resolution, IHandler composer)
        {
            if (resolution == null || (!Many && _resolutions.Count > 0))
                return false;

            var promise = resolution as Promise
                       ?? (resolution as Task)?.ToPromise();

            if (promise != null)
            {
                IsAsync = true;
                if (Many) promise = promise.Catch((ex,s) => null);
                promise = promise.Then((result, s) =>
                {
                    if (RuntimeHelper.IsCollection(result))
                        return ((IEnumerable)result).Cast<object>()
                            .Where(r => r != null && IsSatisfied(r, composer));
                    return result != null && IsSatisfied(result, composer)
                         ? result : null;
                });
                resolution = promise;
            }
            else if (!IsSatisfied(resolution, composer))
                return false;

            _resolutions.Add(resolution);
            return true;
        }

        protected virtual bool IsSatisfied(object resolution, IHandler composer)
        {
            return true;
        }

        bool ICallbackDispatch.Dispatch(Handler handler, bool greedy, IHandler composer)
        {
            var surrogate = handler.Surrogate;
            var handled   = surrogate != null && Implied(surrogate, false, composer);
            if (!handled || greedy)
                handled = Implied(handler, false, composer) || handled;
            if (handled && !greedy) return true;

            var count = _resolutions.Count;
            handled = ProvidesAttribute.Policy.Dispatch(
                handler, this, greedy, composer, r => Resolve(r, composer))
                || handled;
            return handled || (_resolutions.Count > count);
        }

        private bool Implied(object item, bool invariant, IHandler composer)
        {
            var type = Key as Type;
            if (type == null) return false;

            var compatible = invariant
                           ? type == item.GetType()
                           : type.IsInstanceOfType(item);

            return compatible && Resolve(item, composer);
        }

        private static IEnumerable<object> Flatten(IEnumerable<object> collection)
        {
            return collection
                .Where(item => item != null)
                .SelectMany(item => RuntimeHelper.IsCollection(item)
                    ? ((IEnumerable)item).Cast<object>()
                    : new[] {item})
                .Distinct();
        }
    }
}
