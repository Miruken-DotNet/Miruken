namespace Miruken.Validate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Concurrency;

    public class Validation :
        ICallback, IAsyncCallback, IDispatchCallback
    {
        private readonly List<Promise> _asyncResults;
        private object _result;

        public Validation(object target, object[] scopes)
        {
            Target = target 
                  ?? throw new ArgumentNullException(nameof(target));
            Outcome       = new ValidationOutcome();
            ScopeMatcher  = CreateScopeMatcher(scopes);
            _asyncResults = new List<Promise>();
        }

        public object            Target        { get; }
        public ValidationOutcome Outcome       { get; }
        public IScopeMatching    ScopeMatcher  { get; }
        public bool              WantsAsync    { get; set; }
        public bool              IsAsync       { get; private set; }
        public bool              StopOnFailure { get; set; }

        public CallbackPolicy Policy => ValidatesAttribute.Policy;

        public Type ResultType => WantsAsync || IsAsync ? typeof(Promise) : null;

        public object Result
        {
            get
            {
                if (_result == null)
                {
                    if (_asyncResults.Count == 1)
                        _result = _asyncResults[0];
                    else if (_asyncResults.Count > 1)
                        _result = Promise.All(_asyncResults.ToArray());
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

        public bool AddResult(object result, bool strict)
        {
            if (result == null) return false;

            var promise = result as Promise
                       ?? (result as Task)?.ToPromise();

            if (promise != null)
            {
                _asyncResults.Add(promise);
                IsAsync = true;
            }

            _result = null;
            return true;
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            var handled = Policy.Dispatch(
                handler, this, greedy, composer, AddResult);
            if (greedy && StopOnFailure && !Outcome.IsValid)
                greedy = false;
            return handled;
        }

        private static IScopeMatching CreateScopeMatcher(object[] scopes)
        {
            if (scopes == null || scopes.Length == 0)
                return EqualsScopeMatcher.Default;
            if (scopes.Length == 1)
                return scopes[0] as IScopeMatching
                    ?? new EqualsScopeMatcher(scopes[0]);
            return new CompositeScopeMatcher(scopes.Select(scope => 
                CreateScopeMatcher(new [] {scope})).ToArray());
        }
    }
}