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

        public AcceptResultDelegate AcceptResult { get; set; }
        public Func<object, Type>   ResultType   { get; set; }
        public BindMethodDelegate   Binder       { get; set; }

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

        public virtual PolicyMethodBinding BindMethod(
            ref PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            return Binder?.Invoke(this, ref policyMethodBindingInfo)
                ?? new PolicyMethodBinding(this, ref policyMethodBindingInfo);
        }

        public abstract object GetKey(object callback);

        public abstract IEnumerable GetCompatibleKeys(object callback, IEnumerable keys);

        public bool Dispatch(object handler, object callback, bool greedy,
            IHandler composer, ResultsDelegate results = null)
        {
            var descriptor = HandlerDescriptor.GetDescriptor(handler.GetType());
            return descriptor.Dispatch(this, handler, callback, greedy, 
                                       composer, results);
        }

        public IEnumerable<Type> GetHandlers(object callback)
        {
            var key = GetKey(callback);
            return HandlerDescriptor.GetPolicyHandlers(this, key);
        }

        public IEnumerable<PolicyMethodBinding> GetMethods(object callback)
        {
            var key = GetKey(callback);
            return HandlerDescriptor.GetPolicyMethods(this, key);
        }

        public static CallbackPolicy GetCallbackPolicy(object callback)
        {
            var dispatch = callback as IDispatchCallback;
            return dispatch?.Policy ?? HandlesAttribute.Policy;
        }
    }
}