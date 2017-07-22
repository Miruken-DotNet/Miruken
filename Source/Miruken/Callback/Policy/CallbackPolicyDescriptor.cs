namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class CallbackPolicyDescriptor
    {
        private List<PolicyMethodBinding> _unknown;
        private Dictionary<object, HashSet<PolicyMethodBinding>> _indexed;

        public Keys Keys { get; } = new Keys();

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

            var indexed = _indexed ??
                (_indexed = new Dictionary<object, HashSet<PolicyMethodBinding>>());

            HashSet<PolicyMethodBinding> methods;
            if (!indexed.TryGetValue(key, out methods))
            {
                Keys.AddKey(key);
                methods = new HashSet<PolicyMethodBinding>();
                indexed.Add(key, methods);
            }
            methods.Add(method);
        }

        internal ICollection<PolicyMethodBinding> GetMethods(object key)
        {
            HashSet<PolicyMethodBinding> methods = null;
            return _indexed?.TryGetValue(key, out methods) == true
                 ? (ICollection<PolicyMethodBinding>)methods
                 : Array.Empty<PolicyMethodBinding>();
        }

        internal IEnumerable<PolicyMethodBinding>
            SelectMethods(object key, IEnumerable keys)
        {
            if (keys != null && _indexed != null)
            {
                foreach (var next in keys)
                {
                    HashSet<PolicyMethodBinding> methods;

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