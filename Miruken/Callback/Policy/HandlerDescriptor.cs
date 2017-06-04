namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

    public class HandlerDescriptor
    {
        #region PolicyMethods

        private class PolicyMethods
        {
            private List<PolicyMethodBinding> _unknown;
            private Dictionary<object, List<PolicyMethodBinding>> _indexed;

            public ICollection Keys => _indexed?.Keys;

            public void Insert(PolicyMethodBinding method)
            {
                var key = method.GetKey();
                if (key == null)
                {
                    var unknown = _unknown ??
                        (_unknown = new List<PolicyMethodBinding>());
                    unknown.Add(method);
                    return;
                }

                var indexed = _indexed ??
                    (_indexed = new Dictionary<object, List<PolicyMethodBinding>>());

                List<PolicyMethodBinding> methods;
                if (!indexed.TryGetValue(key, out methods))
                {
                    methods = new List<PolicyMethodBinding>();
                    indexed.Add(key, methods);
                }
                methods.Add(method);
            }

            public IEnumerable<PolicyMethodBinding> GetMethods(IEnumerable keys)
            {
                if (keys != null && _indexed != null)
                {
                    foreach (var key in keys)
                    {
                        List<PolicyMethodBinding> methods;

                        if (_indexed.TryGetValue(key, out methods))
                            foreach (var method in methods)
                                yield return method;
                    }
                }

                if (_unknown != null)
                    foreach (var method in _unknown)
                        yield return method;
            }
        }

        #endregion

        private readonly Dictionary<CallbackPolicy, PolicyMethods> _methods;

        private static readonly ConcurrentDictionary<Type, HandlerDescriptor>
            _descriptors = new ConcurrentDictionary<Type, HandlerDescriptor>();

        public HandlerDescriptor(Type type)
        {
            foreach (var method in type.GetMethods(Binding))
            {
                MethodDispatch dispatch = null;

                if (method.IsSpecialName || method.IsFamily ||
                    method.DeclaringType == typeof (object))
                    continue;

                var attributes = (DefinitionAttribute[])
                    Attribute.GetCustomAttributes(method, typeof(DefinitionAttribute), false);

                foreach (var attribute in attributes)
                {
                    var policy = attribute.CallbackPolicy;
                    var rule   = policy.MatchMethod(method, attribute);
                    if (rule == null)
                        throw new InvalidOperationException(
                            $"The policy for {attribute.GetType().FullName} rejected method '{method.GetDescription()}'");

                    dispatch = dispatch ?? new MethodDispatch(method);
                    var binding = rule.Bind(dispatch, attribute);

                    if (_methods == null)
                        _methods = new Dictionary<CallbackPolicy, PolicyMethods>();

                    PolicyMethods methods;
                    if (!_methods.TryGetValue(policy, out methods))
                    {
                        methods = new PolicyMethods();
                        _methods.Add(policy, methods);
                    }

                    methods.Insert(binding);
                }
            }
        }

        internal bool Dispatch(
            CallbackPolicy policy, object target, object callback,
            bool greedy, IHandler composer, Func<object, bool> results = null)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            PolicyMethods methods = null;
            if (_methods?.TryGetValue(policy, out methods) != true)
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

        private const BindingFlags Binding = BindingFlags.Instance 
                                           | BindingFlags.Public 
                                           | BindingFlags.NonPublic;
    }
}