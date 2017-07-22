namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    public class HandlerDescriptor
    {
        private readonly Dictionary<CallbackPolicy, CallbackPolicyDescriptor> _policies;

        private static readonly ConcurrentDictionary<Type, HandlerDescriptor>
            _descriptors = new ConcurrentDictionary<Type, HandlerDescriptor>();

        public HandlerDescriptor(Type type)
        {
            HandlerType = type;
            var members = type.FindMembers(Members, Binding, IsDefinition, null);
            foreach (var member in members)
            {
                MethodDispatch dispatch = null;
                var method = member as MethodInfo
                          ?? ((PropertyInfo)member).GetMethod;
                var attributes = Attribute.GetCustomAttributes(member, false);

                foreach (var definition in attributes.OfType<DefinitionAttribute>())
                {
                    var policy = definition.CallbackPolicy;
                    var rule   = policy.MatchMethod(method, definition);
                    if (rule == null)
                        throw new InvalidOperationException(
                            $"The policy for {definition.GetType().FullName} rejected method '{method.GetDescription()}'");

                    dispatch = dispatch ?? new MethodDispatch(method, attributes);
                    var binding = rule.Bind(dispatch, definition);

                    if (_policies == null)
                        _policies = new Dictionary<CallbackPolicy, CallbackPolicyDescriptor>();

                    CallbackPolicyDescriptor methods;
                    if (!_policies.TryGetValue(policy, out methods))
                    {
                        methods = new CallbackPolicyDescriptor();
                        _policies.Add(policy, methods);
                    }

                    methods.Insert(binding);
                }
            }
        }

        public Type HandlerType { get; }

        public CallbackPolicyDescriptor GetPolicyDescriptor(CallbackPolicy policy)
        {
            CallbackPolicyDescriptor descriptor;
            return _policies.TryGetValue(policy, out descriptor) ? descriptor : null;
        }

        internal bool Dispatch(
            CallbackPolicy policy, object target, object callback,
            bool greedy, IHandler composer, Func<object, bool> results = null)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            CallbackPolicyDescriptor descriptor = null;
            if (_policies?.TryGetValue(policy, out descriptor) != true)
                return false;

            var key = policy.GetKey(callback);
            ICollection<PolicyMethodBinding> keyMethods = null;

            if (!greedy)
            {
                keyMethods = descriptor.GetMethods(key);
                if (keyMethods.Any(method => method?.Dispatch(
                        target, callback, composer, results) == true))
                    return true;
            }

            var dispatched = false;
            var keys = policy.SelectKeys(callback, descriptor.Keys);

            foreach (var method in descriptor.SelectMethods(key, keys))
            {
                if (keyMethods?.Contains(method) == true)
                    continue;
                dispatched = method.Dispatch(target, callback,
                    composer, results) || dispatched;
                if (dispatched && !greedy) return true;
            }

            return dispatched;
        }

        public static HandlerDescriptor GetDescriptor(Type type)
        {
            return _descriptors.GetOrAdd(type, t => new HandlerDescriptor(t));
        }

        private static bool IsDefinition(MemberInfo member, object criteria)
        {
            if (member.DeclaringType == typeof(object))
                return false;
            var method = member as MethodInfo;
            if (method != null)
            {
                if (method.IsSpecialName || method.IsFamily)
                    return false;
            }
            else if (!((PropertyInfo)member).CanRead)
                return false; 
            return member.IsDefined(typeof(DefinitionAttribute));
        }

        private const MemberTypes Members  = MemberTypes.Method 
                                           | MemberTypes.Property;

        private const BindingFlags Binding = BindingFlags.Instance 
                                           | BindingFlags.Public 
                                           | BindingFlags.NonPublic;
    }
}