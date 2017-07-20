namespace Miruken.Callback.Policy
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class CallbackPolicyDescriptor
    {
        private List<PolicyMethodBinding> _unknown;
        private Dictionary<object, List<PolicyMethodBinding>> _indexed;

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
                (_indexed = new Dictionary<object, List<PolicyMethodBinding>>());

            List<PolicyMethodBinding> methods;
            if (!indexed.TryGetValue(key, out methods))
            {
                Keys.AddKey(key);
                methods = new List<PolicyMethodBinding>();
                indexed.Add(key, methods);
            }
            methods.Add(method);
        }

        internal PolicyMethodBinding GetMethod(object key)
        {
            List<PolicyMethodBinding> methods = null;
            return _indexed?.TryGetValue(key, out methods) == true
                 ? methods.FirstOrDefault() : null;
        }

        internal IEnumerable<PolicyMethodBinding> SelectMethods(IEnumerable keys)
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
}