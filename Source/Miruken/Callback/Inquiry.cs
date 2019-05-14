namespace Miruken.Callback
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;
    using Policy.Bindings;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Inquiry : ICallback, IAsyncCallback,
        IDispatchCallback, IDispatchCallbackGuard, IBindingScope
    {
        private readonly List<object> _resolutions;
        private readonly List<object> _promises;
        private object _result;

        public Inquiry(object key, bool many = false)
        {
            Key          = key ?? throw new ArgumentNullException(nameof(key));
            Many         = many;
            Metadata     = new BindingMetadata();
            _resolutions = new List<object>();
            _promises    = new List<object>();
        }

        public Inquiry(object key, Inquiry parent, bool many = false)
            : this(key, many)
        {
            Parent = parent;
        }

        public object  Key        { get; }
        public bool    Many       { get; }
        public Inquiry Parent     { get; }
        public bool    WantsAsync { get; set; }
        public bool    IsAsync    { get; private set; }

        public object          Target     { get; private set; }
        public MemberDispatch  Dispatcher { get; private set; }
        public BindingMetadata Metadata   { get; }

        public CallbackPolicy Policy => Provides.Policy;

        public ICollection<object> Resolutions => _resolutions.AsReadOnly();

        public Type ResultType =>
            WantsAsync || IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result == null)
                {
                    if (IsAsync)
                    {
                        _result = Many
                            ? Promise.All(_promises)
                                .Then((_, s) => _resolutions.ToArray())
                            : (object)Promise.All(_promises)
                                .Then((_, s) => _resolutions.FirstOrDefault());
                    }
                    else
                    {
                        _result = Many ? _resolutions.ToArray()
                            : _resolutions.FirstOrDefault();
                    }
                }

                if (IsAsync)
                {
                    if (!WantsAsync)
                        _result = (_result as Promise)?.Wait();
                }
                else if (WantsAsync)
                {
                    if (Many)
                        _result = Promise.Resolved(_result as object[]);
                    else
                        _result = Promise.Resolved(_result);
                }

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
            bool resolved;
            if (resolution == null) return false;
            if (!strict && resolution is object[] array)
            {
                resolved = array.Aggregate(false,
                    (s, res) => Include(res, false, greedy, composer) || s);
            }
            else if (!strict && resolution is ICollection collection)
            {
                resolved = collection.Cast<object>().Aggregate(false,
                    (s, res) => Include(res, false, greedy, composer) || s);
            }
            else
            {
                resolved = Include(resolution, strict, greedy, composer);
            }
            if (resolved) _result = null;
            return resolved;
        }

        private bool Include(object resolution, bool strict,
            bool greedy, IHandler composer)
        {
            if (resolution == null) return false;

            var promise = resolution as Promise
                       ?? (resolution as Task)?.ToPromise();

            if (promise?.State == PromiseState.Fulfilled)
            {
                resolution = promise.Wait();
                promise    = null;
            }

            if (promise != null)
            {
                IsAsync = true;
                _promises.Add(promise.Then((result, s) =>
                {
                    switch (result)
                    {
                        case object[] array:
                            _resolutions.AddRange(array.Where(res =>
                                res != null && IsSatisfied(res, greedy, composer)));
                            break;
                        case ICollection collection:
                            _resolutions.AddRange(collection.Cast<object>().Where(res =>
                                res != null && IsSatisfied(res, greedy, composer)));
                            break;
                        default:
                            _resolutions.Add(result);
                            break;
                    }
                }).Catch((_, s) => (object)null));
            }
            else if (!IsSatisfied(resolution, greedy, composer))
                return false;
            else if (strict)
            {
                _resolutions.Add(resolution);
            }
            else switch (resolution)
            {
                case object[] array:
                    _resolutions.AddRange(array.Where(res =>
                        res != null && IsSatisfied(res, greedy, composer)));
                    break;
                case ICollection collection:
                    _resolutions.AddRange(collection.Cast<object>().Where(res =>
                        res != null && IsSatisfied(res, greedy, composer)));
                    break;
                default:
                    _resolutions.Add(resolution);
                    break;
            }
            return true;
        }

        protected virtual bool IsSatisfied(
            object resolution, bool greedy, IHandler composer)
        {
            return true;
        }

        public virtual bool CanDispatch(
            object target, MemberDispatch dispatcher)
        {
            if (InProgress(target, dispatcher)) return false;
            Target     = target;
            Dispatcher = dispatcher;
            return true;
        }

        public virtual bool Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            try
            {
                var isGreedy = greedy;
                var handled  = Implied(handler, isGreedy, composer);
                if (handled && !greedy) return true;

                var count = _resolutions.Count + _promises.Count;
                handled = Policy.Dispatch(handler, this, greedy, composer,
                    (r, strict) => Resolve(r, strict, isGreedy, composer)) || handled;
                return handled || (_resolutions.Count + _promises.Count> count);
            }
            finally
            {
                Dispatcher = null;
                Target     = null;
            }
        }

        private bool Implied(object item, bool greedy, IHandler composer)
        {
            if (item == null || !(Key is Type type) || !Metadata.IsEmpty)
                return false;
            var compatible =  type.IsInstanceOfType(item);
            return compatible && Resolve(item, false, greedy, composer);
        }

        private bool InProgress(object target, MemberDispatch dispatcher)
        {
            return ReferenceEquals(target, Target) &&
                   ReferenceEquals(dispatcher, Dispatcher) ||
                   Parent?.InProgress(target, dispatcher) == true;
        }

        private string DebuggerDisplay
        {
            get
            {
                var many = Many ? "many " : "";
                return $"Inquiry {many}| {Key}";
            }
        }
    }
}
