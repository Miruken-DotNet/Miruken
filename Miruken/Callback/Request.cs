namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;

    public class Request : ICallback, ICallbackDispatch
    {
        private readonly List<object> _responses;
        private object _result;

        public Request(object callback, bool many = false)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            Callback   = callback;
            Many       = many;
            _responses = new List<object>();
        }

        public bool            Many     { get; }
        public object          Callback { get; }
        public bool            IsAsync  { get; private set; }
        public CallbackPolicy  Policy   { get; set; }
        public ICollection<object> Responses => _responses.AsReadOnly();

        public Type ResultType => IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result != null) return _result;
                if (!Many)
                {
                    if (_responses.Count > 0)
                        _result = _responses[0];
                }
                else if (IsAsync)
                {
                    _result = Promise.All(_responses
                        .Select(r => (r as Promise) ?? Promise.Resolved(r))
                        .ToArray())
                        .Then((results, s) => results.Where(r => r != null));
                }
                else
                    _result = _responses;
                return _result;
            }
            set { _result = value; }
        }

        public bool Respond(object response, IHandler composer)
        {
            if (response == null || (!Many && _responses.Count > 0))
                return false;

            var promise = response as Promise
                       ?? (response as Task)?.ToPromise();

            if (promise != null)
            {
                response = promise;
                IsAsync  = true;
            }

            _responses.Add(response);
            _result = null;
            return true;
        }

        bool ICallbackDispatch.Dispatch(Handler handler, bool greedy, IHandler composer)
        {
            var handled = false;
            var count   = _responses.Count;
            if (Policy == null)
            {
                handled = HandlesAttribute.Policy.Dispatch(
                    handler, Callback, greedy, composer, r => Respond(r, composer));
                if (!greedy && (handled || _responses.Count > count))
                    return true;
            }
            var policy = Policy ?? HandlesAttribute.Policy;
            return policy.Dispatch(handler, this, greedy, composer, r => Respond(r, composer))
                || handled || (_responses.Count > count);
        }
    }
}
