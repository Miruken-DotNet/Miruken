﻿namespace Miruken.Callback
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
    public sealed class Command 
        : ICallback, IAsyncCallback,
          IFilterCallback, IBatchCallback,
          IDispatchCallback, IDispatchCallbackGuard
    {
        private CallbackPolicy _policy;
        private readonly List<object> _results;
        private readonly List<object> _promises;
        private object _result;

        public Command(object callback, bool many = false)
        {
            Callback  = callback 
                     ?? throw new ArgumentNullException(nameof(callback));
            Many      = many;
            _results  = new List<object>();
            _promises = new List<object>();
        }

        public bool   Many       { get; }
        public object Callback   { get; }
        public bool   WantsAsync { get; set; }
        public bool   IsAsync    { get; private set; }

        public CallbackPolicy Policy
        {
            get => _policy ?? Handles.Policy;
            set => _policy = value;
        }

        public ICollection<object> Results => _results.AsReadOnly();

        public Type ResultType => WantsAsync || IsAsync ? typeof(Promise) : null;

        bool IFilterCallback.CanFilter =>
            (Callback as IFilterCallback)?.CanFilter != false;

        bool IBatchCallback.CanBatch =>
            (Callback as IBatchCallback)?.CanBatch != false;

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
                                    .Then((_, _) => _results.ToArray())
                                : (object)Promise.All(_promises)
                                    .Then((_, _) => _results.FirstOrDefault());
                    }
                    else
                    {
                        _result = Many ? _results.ToArray() : _results.FirstOrDefault();
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

        public bool Respond(object response, bool strict, int? priority = null)
        {
            if (response == null) return false;
            var promise = response as Promise
                          ?? (response as Task)?.ToPromise();

            if (promise?.State == PromiseState.Fulfilled)
            {
                response = promise.Wait();
                promise = null;
            }

            if (promise != null)
            {
                IsAsync = true;
                _promises.Add(promise.Then((result, _) =>
                {
                    if (result != null) _results.Add(result);
                }));
            }
            else
                _results.Add(response);

            _result = null;
            return true;
        }


        public bool CanDispatch(object target,
            PolicyMemberBinding binding, MemberDispatch dispatcher,
            out IDisposable reset)
        {
            reset = null;
            return (Callback as IDispatchCallbackGuard)
                   ?.CanDispatch(target, binding, dispatcher, out reset) != false;
        }

        public bool Dispatch(object handler, ref bool greedy, IHandler composer)
        {
            var count = _results.Count;
            return Policy.Dispatch(handler, this, greedy, composer, Respond)
                || _results.Count > count;
        }

        private string DebuggerDisplay
        {
            get
            {
                var many = Many ? "many " : "";
                return $"Command {many}| {Callback}";
            }
        }
    }
}
