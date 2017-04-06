namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Policy;

    public class HandlerDescriptor
    {
        #region PolicyMethods

        private class PolicyMethods
        {
            private List<MethodDefinition> _untypedMethods;
            private LinkedList<MethodDefinition> _typedMethods;
            private Dictionary<Type, LinkedListNode<MethodDefinition>> _indexes;

            public void Insert(MethodDefinition method)
            {
                if (method.Untyped)
                {
                    var untyped = _untypedMethods
                               ?? (_untypedMethods = new List<MethodDefinition>());
                    untyped.Add(method);
                    return;
                }
                var typed  = _typedMethods
                          ?? (_typedMethods = new LinkedList<MethodDefinition>());
                var type   = method.VarianceType;
                var first  = GetFirst(type);
                var insert = first ?? typed.First;
                LinkedListNode<MethodDefinition> node;

                while (true)
                {
                    if (insert == null)
                    {
                        node = typed.AddLast(method);
                        break;
                    }
                    if (method.CompareTo(insert.Value) < 0)
                    {
                        node = typed.AddBefore(insert, method);
                        break;
                    }
                    insert = insert.Next;
                }
 
                if (first == null)
                {
                    var indexes = _indexes
                               ?? (_indexes = new Dictionary<Type, 
                                   LinkedListNode<MethodDefinition>>());
                    indexes[type] = node;
                }
            }

            public IEnumerable<MethodDefinition> GetMethods(Type type)
            {
                if (_typedMethods != null)
                {
                    var node = GetFirst(type) ?? _typedMethods.First;
                    while (node != null)
                    {
                        yield return node.Value;
                        node = node.Next;
                    }
                }
                if (_untypedMethods != null)
                    foreach (var method in _untypedMethods)
                        yield return method;
            }

            private LinkedListNode<MethodDefinition> GetFirst(Type type)
            {
                if (_indexes == null || type == null) return null;
                LinkedListNode<MethodDefinition> first;
                return _indexes.TryGetValue(type, out first) ? first : null;
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
            var varianceType = policy.GetVarianceType(callback);

            try
            {
                foreach (var method in methods.GetMethods(varianceType))
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