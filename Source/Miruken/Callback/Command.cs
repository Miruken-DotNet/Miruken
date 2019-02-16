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
    public sealed class Command 
        : ICallback, IAsyncCallback,
            IFilterCallback, IBatchCallback,
            IDispatchCallback
    {
        private CallbackPolicy _policy;
        private readonly List<object> _results;
        private object _result;

        public Command(object callback, bool many = false)
        {
            Callback = callback 
                ?? throw new ArgumentNullException(nameof(callback));
            Many     = many;
            _results = new List<object>();
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
                    if (!Many)
                    {
                        if (_results.Count > 0)
                            _result = _results[0];
                    }
                    else if (IsAsync)
                    {
                        _result = Promise.All(_results
                                .Select(r => (r as Promise) ?? Promise.Resolved(r))
                                .ToArray())
                            .Then((results, s) => results.Where(r => r != null)
                                .ToArray());
                    }
                    else
                        _result = _results;
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

        public bool Respond(object response, bool strict)
        {
            if (response == null || (!Many && _results.Count > 0))
                return false;

            var promise = response as Promise
                       ?? (response as Task)?.ToPromise();

            if (promise != null)
            {
                response = promise;
                IsAsync  = true;
            }

            _results.Add(response);
            _result = null;
            return true;
        }

        public bool Dispatch(object handler, ref bool greedy, IHandler composer)
        {
            var count = _results.Count;
            return Policy.Dispatch(handler, this, greedy, composer, Respond)
                || (_results.Count > count);
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
