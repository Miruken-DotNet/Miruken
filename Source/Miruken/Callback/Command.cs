namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;

    public class Command 
        : ICallback, IAsyncCallback, IDispatchCallback
    {
        private readonly List<object> _results;
        private object _result;

        public Command(object callback, bool many = false)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            Callback = callback;
            Many     = many;
            _results = new List<object>();
        }

        public bool            Many       { get; }
        public object          Callback   { get; }
        public bool            WantsAsync { get; set; }
        public bool            IsAsync    { get; private set; }
        public CallbackPolicy  Policy     { get; set; }

        public ICollection<object> Results => _results.AsReadOnly();

        public Type ResultType => WantsAsync || IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result != null) return _result;
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

        public bool Respond(object response)
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

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            var count = _results.Count;
            return (Policy ?? HandlesAttribute.Policy)
                .Dispatch(handler, this, greedy, composer, Respond)
                    || (_results.Count > count);
        }
    }
}
