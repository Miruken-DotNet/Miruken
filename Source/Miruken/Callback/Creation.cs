namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Creation : ICallback, IAsyncCallback, IDispatchCallback
    {
        private readonly List<object> _instances;
        private readonly List<object> _promises;
        private object _result;

        public Creation(Type type, bool many = false)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Many = many;

            _instances = new List<object>();
            _promises  = new List<object>();
        }

        public Type Type       { get; }
        public bool Many       { get; }
        public bool WantsAsync { get; set; }
        public bool IsAsync    { get; private set; }

        public CallbackPolicy Policy => Creates.Policy;

        public ICollection<object> Instances => _instances.AsReadOnly();

        public Type ResultType => WantsAsync
                               || IsAsync ? typeof(Promise) : null;

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
                                .Then((_, _) => _instances.ToArray())
                            : (object)Promise.All(_promises)
                                .Then((_, _) => _instances.FirstOrDefault());
                    }
                    else
                    {
                        _result = Many ? _instances.ToArray()
                                : _instances.FirstOrDefault();
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

        public bool AddInstance(object instance, bool strict, int? priority = null)
        {
            if (instance == null)
                return false;

            var promise = instance as Promise
                       ?? (instance as Task)?.ToPromise();

            if (promise?.State == PromiseState.Fulfilled)
            {
                instance = promise.Wait();
                promise  = null;
            }

            if (promise != null)
            {
                IsAsync = true;
                _promises.Add(promise.Then((result, _) =>
                {
                    _instances.Add(result);
                }).Catch((_, _) => (object)null));
            }
            else
            {
                _instances.Add(instance);
            }

            _result = null;

            return true;
        }

        public bool Dispatch(object handler, ref bool greedy, IHandler composer)
        {
            var count   = _instances.Count + _promises.Count;
            var handled = Policy.Dispatch(handler, this, greedy, composer, AddInstance);
            return handled || _instances.Count + _promises.Count > count;
        }

        private string DebuggerDisplay => $"Create | {Type}";
    }
}