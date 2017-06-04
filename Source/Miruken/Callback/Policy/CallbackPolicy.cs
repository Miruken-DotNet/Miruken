namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public abstract class CallbackPolicy
    {
        private readonly List<MethodRule> _rules = new List<MethodRule>();
        private readonly List<IFilterProvider> _filters = new List<IFilterProvider>();

        public AcceptResultDelegate AcceptResult     { get; set; }
        public Func<object, Type>   ResultType       { get; set; }
        public BindMethodDelegate   Binder           { get; set; }
        public bool                 UseTargetFilters { get; set; }

        public IEnumerable<IFilterProvider> Filters => _filters;

        public void AddMethodRule(MethodRule rule)
        {
            _rules.Add(rule);
        }

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            _filters.AddRange(providers.Where(p => p != null));
        }

        public MethodRule MatchMethod(MethodInfo method, DefinitionAttribute attribute)
        {
            return _rules.FirstOrDefault(r => r.Matches(method, attribute));
        }

        public virtual PolicyMethodBinding BindMethod(MethodRule rule,
            MethodDispatch dispatch, DefinitionAttribute attribute)
        {
            return Binder?.Invoke(rule, dispatch, attribute, this)
                ?? new PolicyMethodBinding(rule, dispatch, attribute, this);
        }

        public abstract IEnumerable SelectKeys(object callback, ICollection keys);

        public bool Dispatch(Handler handler, object callback, bool greedy,
            IHandler composer, Func<object, bool> results = null)
        {
            var handled   = false;
            var surrogate = handler.Surrogate;

            if (surrogate != null)
            {
                var descriptor = HandlerDescriptor.GetDescriptor(surrogate.GetType());
                handled = descriptor.Dispatch(
                    this, surrogate, callback, greedy, composer, results);
            }

            if (!handled || greedy)
            {
                var descriptor = HandlerDescriptor.GetDescriptor(handler.GetType());
                handled = descriptor.Dispatch(
                    this, handler, callback, greedy, composer, results)
                       || handled;
            }

            return handled;
        }
    }
}