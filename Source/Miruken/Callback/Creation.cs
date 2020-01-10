namespace Miruken.Callback
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Creation :
        ICallback, IAsyncCallback, IDispatchCallback
    {
        private Promise _asyncResult;
        private object _result;

        public Creation(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));

            if (!type.IsClass || type.IsAbstract)
                throw new ArgumentException("Only concrete classes can be created");
        }

        public Type Type       { get; }
        public bool WantsAsync { get; set; }
        public bool IsAsync    { get; private set; }

        public CallbackPolicy Policy => Creates.Policy;

        public Type ResultType => WantsAsync
                               || IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result == null)
                    _result = _asyncResult;

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

        public bool AddResult(object result, bool strict, int? priority = null)
        {
            if (result == null || _result != null || _asyncResult != null)
                return false;

            _asyncResult = result as Promise
                       ?? (result as Task)?.ToPromise();

            if (_asyncResult != null)
            {
                if (_asyncResult?.State == PromiseState.Fulfilled)
                {
                    _result      = _asyncResult.Wait();
                    _asyncResult = null;
                }
                else
                {

                    IsAsync = true;
                    _result = null;
                }
            }
            else
            {
                _result = result;
            }

            return true;
        }

        public bool Dispatch(object handler, ref bool _, IHandler composer)
        {
            return Policy.Dispatch(handler, this, false, composer, AddResult);
        }

        private string DebuggerDisplay => $"Create | {Type}";
    }
}