namespace Miruken.Callback
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using Policy;

    public class HandlerDescriptor
    {
        #region PolicyMethods

        private class PolicyMethods
        {
            private List<MethodDefinition> _unknown;
            private Dictionary<object, List<MethodDefinition>> _indexed;

            public ICollection Keys => _indexed?.Keys;

            public void Insert(MethodDefinition method)
            {
                var key = method.GetKey();
                if (key == null)
                {
                    var unknown = _unknown ??
                        (_unknown = new List<MethodDefinition>());
                    unknown.Add(method);
                    return;
                }

                var indexed = _indexed ??
                    (_indexed = new Dictionary<object, List<MethodDefinition>>());

                List<MethodDefinition> methods;
                if (!indexed.TryGetValue(key, out methods))
                {
                    methods = new List<MethodDefinition>();
                    indexed.Add(key, methods);
                }
                methods.Add(method);
            }

            public IEnumerable<MethodDefinition> GetMethods(IEnumerable keys)
            {
                if (keys != null && _indexed != null)
                {
                    foreach (var key in keys)
                    {
                        List<MethodDefinition> methods;

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

        public HandlerDescriptor(IReflect type)
        {
            foreach (var method in type.GetMethods(Binding))
            {
                if (method.IsSpecialName || method.IsFamily ||
                    method.DeclaringType == typeof (object))
                    continue;

                var attributes = (DefinitionAttribute[])
                    Attribute.GetCustomAttributes(method, typeof(DefinitionAttribute));

                foreach (var attribute in attributes)
                {
                    var definition = attribute.MatchMethod(method);
                    if (definition == null)
                        throw new InvalidOperationException(
                            $"The policy for {attribute.GetType().FullName} rejected method '{GetDescription(method)}'");

                    if (_methods == null)
                        _methods = new Dictionary<CallbackPolicy, PolicyMethods>();

                    PolicyMethods methods;
                    var policy = attribute.MethodPolicy;
                    if (!_methods.TryGetValue(policy, out methods))
                    {
                        methods = new PolicyMethods();
                        _methods.Add(policy, methods);
                    }

                    methods.Insert(definition);
                }
            }
        }

        internal bool Dispatch(
            CallbackPolicy policy, object target, object callback,
            bool greedy, IHandler composer)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            if (!policy.Accepts(callback, composer)) return false;

            PolicyMethods methods = null;
            if (_methods?.TryGetValue(policy, out methods) != true)
                return false;

            var dispatched   = false;
            var oldUnhandled = HandleMethod.Unhandled;
            var indexes      = methods.Keys;
            var keys         = indexes == null ? null 
                             : policy.SelectKeys(callback, indexes);

            try
            {
                foreach (var method in methods.GetMethods(keys))
                {
                    HandleMethod.Unhandled = false;
                    var handled = method.Dispatch(target, callback, composer);
                    dispatched = (handled && !HandleMethod.Unhandled) || dispatched;
                    if (dispatched && !greedy) return true;
                }
            }
            finally
            {
                HandleMethod.Unhandled = oldUnhandled;
            }

            return dispatched;
        }

        private static string GetDescription(MethodInfo method)
        {
            return $"{method.ReflectedType?.FullName}:{method.Name}";
        }

        public const BindingFlags Binding = BindingFlags.Instance 
                                          | BindingFlags.Public 
                                          | BindingFlags.NonPublic;
    }
}