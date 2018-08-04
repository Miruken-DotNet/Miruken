namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public delegate bool AcceptResultDelegate(object result, MemberBinding binding);

    public abstract class CallbackPolicy 
        : FilteredObject, IComparer<PolicyMemberBinding>, IComparer<object>
    {
        private readonly List<MethodRule> _rules = new List<MethodRule>();

        public AcceptResultDelegate AcceptResult { get; set; }
        public Func<object, Type>   ResultType   { get; set; }
        public BindMemberDelegate   Binder       { get; set; }

        public void AddMethodRule(MethodRule rule)
        {
            _rules.Add(rule);
        }

        public MethodRule MatchMethod(MethodInfo method, CategoryAttribute category)
        {
            return _rules.FirstOrDefault(r => r.Matches(method, category));
        }

        public virtual PolicyMemberBinding BindMethod(
            PolicyMemberBindingInfo policyMemberBindingInfo)
        {
            return Binder?.Invoke(this, policyMemberBindingInfo)
                ?? new PolicyMemberBinding(this, policyMemberBindingInfo);
        }

        public virtual object CreateKey(PolicyMemberBindingInfo bindingInfo)
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

        public int Compare(PolicyMemberBinding x, PolicyMemberBinding y)
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

        public IEnumerable<PolicyMemberBinding> GetMethods()
        {
            return HandlerDescriptor.GetPolicyMethods(this);
        }

        public IEnumerable<PolicyMemberBinding> GetMethods<T>()
        {
            return HandlerDescriptor.GetPolicyMethods<T>(this);
        }

        public static IEnumerable<Type> GetInstanceHandlers(object callback)
        {
            var policy = GetCallbackPolicy(callback);
            return HandlerDescriptor.GetInstanceHandlers(policy, callback);
        }

        public static IEnumerable<Type> GetStaticHandlers(object callback)
        {
            var policy = GetCallbackPolicy(callback);
            return HandlerDescriptor.GetStaticHandlers(policy, callback);
        }

        public static IEnumerable<Type> GetCallbackHandlers(object callback)
        {
            var policy = GetCallbackPolicy(callback);
            return HandlerDescriptor.GetCallbackHandlers(policy, callback);
        }

        public static CallbackPolicy GetCallbackPolicy(object callback)
        {
            var dispatch = callback as IDispatchCallback;
            return dispatch?.Policy ?? Handles.Policy;
        }
    }
}