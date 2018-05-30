namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public delegate bool AcceptResultDelegate(object result, MethodBinding binding);

    public abstract class CallbackPolicy 
        : IComparer<PolicyMethodBinding>, IComparer<object>
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

        public void AddFilters(params Type[] filterTypes)
        {
            if (filterTypes == null || filterTypes.Length == 0) return;
            AddFilters(new FilterAttribute(filterTypes));
        }

        public MethodRule MatchMethod(MethodInfo method, CategoryAttribute category)
        {
            return _rules.FirstOrDefault(r => r.Matches(method, category));
        }

        public virtual PolicyMethodBinding BindMethod(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            return Binder?.Invoke(this, policyMethodBindingInfo)
                ?? new PolicyMethodBinding(this, policyMethodBindingInfo);
        }

        public virtual object CreateKey(PolicyMethodBindingInfo bindingInfo)
        {
            return bindingInfo.InKey != null
                 ? NormalizeKey(bindingInfo.InKey)
                 : NormalizeKey(bindingInfo.OutKey);
        }

        protected object NormalizeKey(object key)
        {
            var varianceType = key as Type;
            if (varianceType == null) return key;
            if (varianceType.ContainsGenericParameters &&
                !varianceType.IsGenericTypeDefinition &&
                !varianceType.IsGenericParameter)
                varianceType = varianceType.GetGenericTypeDefinition();
            return varianceType;
        }

        public abstract object GetKey(object callback);

        public abstract IEnumerable<object> GetCompatibleKeys(
            object key, IEnumerable available);

        public abstract int Compare(object key1, object key2);

        public int Compare(PolicyMethodBinding x, PolicyMethodBinding y)
        {
            return Compare(x?.Key, y?.Key);
        }

        public bool Dispatch(object handler, object callback, bool greedy,
            IHandler composer, ResultsDelegate results = null)
        {
            var descriptor = HandlerDescriptor.GetDescriptor(handler.GetType());
            return descriptor.Dispatch(this, handler, callback, greedy, 
                                       composer, results);
        }

        public IEnumerable<PolicyMethodBinding> GetMethods()
        {
            return HandlerDescriptor.GetPolicyMethods(this);
        }

        public IEnumerable<PolicyMethodBinding> GetMethods<T>()
        {
            return HandlerDescriptor.GetPolicyMethods<T>(this);
        }

        public IEnumerable<PolicyMethodBinding> GetMethods(object key)
        {
            return HandlerDescriptor.GetPolicyMethods(this, key);
        }

        public static IEnumerable<Type> GetCallbackHandlers(object callback)
        {
            var policy = GetCallbackPolicy(callback);
            return HandlerDescriptor.GetCallbackHandlers(policy, callback);
        }

        public static IEnumerable<PolicyMethodBinding> GetCallbackMethods(object callback)
        {
            var policy = GetCallbackPolicy(callback);
            return policy.GetMethods(policy.GetKey(callback));
        }

        public static CallbackPolicy GetCallbackPolicy(object callback)
        {
            var dispatch = callback as IDispatchCallback;
            return dispatch?.Policy ?? HandlesAttribute.Policy;
        }
    }
}