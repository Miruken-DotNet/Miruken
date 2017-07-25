namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CallbackPolicyDescriptor
    {
        private List<PolicyMethodBinding> _unknown;
        private Dictionary<Type, List<PolicyMethodBinding>> _typed;
        private Dictionary<object, List<PolicyMethodBinding>> _indexed;

        public CallbackPolicyDescriptor(CallbackPolicy policy)
        {
            Policy = policy;
        }

        public CallbackPolicy Policy { get; }

        internal void Insert(PolicyMethodBinding method)
        {
            var key = method.Key;
            if (key == null)
            {
                var unknown = _unknown ??
                    (_unknown = new List<PolicyMethodBinding>());
                unknown.Add(method);
                return;
            }

            var type = key as Type;
            List<PolicyMethodBinding> methods;

            if (type != null)
            {
                var typed = _typed ??
                    (_typed = new Dictionary<Type, List<PolicyMethodBinding>>());

                if (!typed.TryGetValue(type, out methods))
                {
                    methods = new List<PolicyMethodBinding>();
                    typed.Add(type, methods);
                }
            }
            else
            {
                var indexed = _indexed ??
                    (_indexed = new Dictionary<object, List<PolicyMethodBinding>>());

                if (!indexed.TryGetValue(key, out methods))
                {
                    methods = new List<PolicyMethodBinding>();
                    indexed.Add(key, methods);
                }
            }

            methods.Add(method);
        }

        internal ICollection<PolicyMethodBinding> GetInvariantMethods(object key)
        {
            var type = key as Type;
            List<PolicyMethodBinding> methods = null;
            if (type != null)
                _typed?.TryGetValue(type, out methods);
            else
                _indexed?.TryGetValue(key, out methods);
            return methods != null
                 ? (ICollection<PolicyMethodBinding>)methods
                 : Array.Empty<PolicyMethodBinding>();
        }

        internal IEnumerable<PolicyMethodBinding> GetCompatibleMethods(object key)
        {
            var type = key as Type;
            if (type != null)
            {
                if (_typed != null)
                {
                    var keys = Policy.CompatibleKeys(key, _typed.Keys);
                    foreach (var next in keys.OfType<Type>())
                    {
                        List<PolicyMethodBinding> methods;
                        if (_typed.TryGetValue(next, out methods))
                            foreach (var method in methods)
                                yield return method;
                    }
                }
            }
            else if (_indexed != null)
            {
                var keys = Policy.CompatibleKeys(key, _indexed.Keys);
                foreach (var next in keys)
                {
                    List<PolicyMethodBinding> methods;
                    if (_indexed.TryGetValue(next, out methods))
                        foreach (var method in methods)
                            yield return method;
                }
            }

            if (_unknown != null)
                foreach (var method in _unknown)
                    yield return method;
        }
    }
}