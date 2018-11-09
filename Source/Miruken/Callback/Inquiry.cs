namespace Miruken.Callback
{
    using System;
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
        private object _result;
        private readonly List<object> _resolutions;

        public Inquiry(object key, bool many = false)
        {
            Key          = key ?? throw new ArgumentNullException(nameof(key));
            Many         = many;
            Metadata     = new BindingMetadata();
            _resolutions = new List<object>();
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

        public ICollection<object> Resolutions =>
            _resolutions.AsReadOnly();

        public Type ResultType =>
            WantsAsync || IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result == null)
                {
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
                }

                if (IsAsync)
                {
                    if (!WantsAsync)
                        _result = (_result as Promise)?.Wait();
                }
                else if (WantsAsync)
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
                (s, res) => Include(res, false, greedy, composer) || s) 
                         ?? Include(resolution, strict, greedy, composer);
            if (resolved) _result = null;
            return resolved;
        }

        private bool Include(object resolution, bool strict,
            bool greedy, IHandler composer)
        {
            if (resolution == null || (!Many && _resolutions.Count > 0))
                return false;

            var promise = resolution as Promise
                       ?? (resolution as Task)?.ToPromise();

            if (promise != null)
            {
                IsAsync = true;
                if (Many)
                    promise = promise.Catch((ex,s) => (object)null);
                resolution = promise.Then((result, s) =>
                {
                    var array = strict ? null : result as object[];
                    return array?.Where(res => IsSatisfied(res, greedy, composer))
                               .ToArray() ?? result;
                });
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

                var count = _resolutions.Count;
                handled = Policy.Dispatch(handler, this, greedy, composer,
                    (r, strict) => Resolve(r, strict, isGreedy, composer)) || handled;
                return handled || (_resolutions.Count > count);
            }
            finally
            {
                Dispatcher = null;
                Target     = null;
            }
        }

        private bool Implied(object item, bool greedy, IHandler composer)
        {
            if (item == null || !(Key is Type type)) return false;
            var compatible =  type.IsInstanceOfType(item);
            return compatible && Resolve(item, false, greedy, composer);
        }

        private bool InProgress(object target, MemberDispatch dispatcher)
        {
            return ReferenceEquals(target, Target) &&
                   ReferenceEquals(dispatcher, Dispatcher) ||
                   Parent?.InProgress(target, dispatcher) == true;
        }

        private static IEnumerable<object> Flatten(IEnumerable<object> collection)
        {
            return collection
                .Where(item => item != null)
                .SelectMany(item => item as object[] ?? new[] {item})
                .Distinct();
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
