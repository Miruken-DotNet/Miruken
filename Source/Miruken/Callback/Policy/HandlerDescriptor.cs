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

        public HandlerDescriptor(Type type)
        {
            HandlerType = type;
            foreach (var method in type.GetMethods(Binding))
            {
                MethodDispatch dispatch = null;

                if (method.IsSpecialName || method.IsFamily ||
                    method.DeclaringType == typeof(object))
                    continue;

                var attributes = Attribute.GetCustomAttributes(method, false);

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

            CallbackPolicyDescriptor methods = null;
            if (_policies?.TryGetValue(policy, out methods) != true)
                return false;

            var dispatched = false;
            var indexes    = methods.Keys;
            var keys       = indexes == null ? null 
                           : policy.SelectKeys(callback, indexes);

            foreach (var method in methods.GetMethods(keys))
            {
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

        private static readonly ConcurrentDictionary<Type, HandlerDescriptor>
            _descriptors = new ConcurrentDictionary<Type, HandlerDescriptor>();

        private const BindingFlags Binding = BindingFlags.Instance 
                                           | BindingFlags.Public 
                                           | BindingFlags.NonPublic;
    }
}