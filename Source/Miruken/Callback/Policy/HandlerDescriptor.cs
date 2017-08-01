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

        private static readonly ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>
            _descriptors = new ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>();

         public HandlerDescriptor(Type handlerType)
        {
            HandlerType = handlerType;
            var members = handlerType.FindMembers(Members, Binding, IsDefinition, null);
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

                    CallbackPolicyDescriptor descriptor;
                    if (!_policies.TryGetValue(policy, out descriptor))
                    {
                        descriptor = new CallbackPolicyDescriptor(policy);
                        _policies.Add(policy, descriptor);
                    }

                    descriptor.Add(binding, handlerType);
                }
            }
        }

        public Type HandlerType { get; }


        internal bool Dispatch(
            CallbackPolicy policy, object target, object callback,
            bool greedy, IHandler composer, Func<object, bool> results = null)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            CallbackPolicyDescriptor descriptor = null;
            if (_policies?.TryGetValue(policy, out descriptor) != true)
                return false;

            var dispatched = false;
            var key        = policy.GetKey(callback);

            foreach (var method in descriptor.GetInvariantMethods(key))
            {
                dispatched = method.Dispatch(
                    target, callback, composer, results) 
                    || dispatched;
                if (dispatched && !greedy) return true;
            }

            foreach (var method in descriptor.GetCompatibleMethods(key))
            {
                dispatched = method.Dispatch(
                    target, callback, composer, results) 
                    || dispatched;
                if (dispatched && !greedy) return true;
            }

            return dispatched;
        }

        public static HandlerDescriptor GetDescriptor<T>()
        {
            return GetDescriptor(typeof(T));
        }

        public static HandlerDescriptor GetDescriptor(Type type)
        {
            try
            {
                return _descriptors.GetOrAdd(type,
                    t => new Lazy<HandlerDescriptor>(() => new HandlerDescriptor(t)))
                    .Value;
            }
            catch
            {
                Lazy<HandlerDescriptor> descriptor;
                _descriptors.TryRemove(type, out descriptor);
                throw;
            }
        }

        public static IEnumerable<Type> GetPolicyHandlers(CallbackPolicy policy, object key)
        {
            foreach (var descriptor in _descriptors.Values)
            {
                var handler = descriptor.Value;
                CallbackPolicyDescriptor policyDescriptor = null;
                if (handler._policies?.TryGetValue(policy, out policyDescriptor) == true)
                {
                    if (policyDescriptor.GetInvariantMethods(key).Any() ||
                        policyDescriptor.GetCompatibleMethods(key).Any())
                        yield return handler.HandlerType;
                }
            }
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