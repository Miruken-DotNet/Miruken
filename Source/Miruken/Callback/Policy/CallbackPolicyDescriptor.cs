namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public class CallbackPolicyDescriptor
    {
        private readonly Dictionary<Type, List<PolicyMethodBinding>> _typed;
        private readonly ConcurrentDictionary
            <object, List<PolicyMethodBinding>> _compatible;
        private Dictionary<object, List<PolicyMethodBinding>> _indexed;
        private List<PolicyMethodBinding> _unknown;

        public CallbackPolicyDescriptor(CallbackPolicy policy)
        {
            Policy      = policy;
            _typed      = new Dictionary<Type, List<PolicyMethodBinding>>();
            _compatible = new ConcurrentDictionary
                <object, List<PolicyMethodBinding>>();
        }

        public CallbackPolicy Policy { get; }

        internal void Add(PolicyMethodBinding method)
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
                if (!_typed.TryGetValue(type, out methods))
                {
                    methods = new List<PolicyMethodBinding>();
                    _typed.Add(type, methods);
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

        internal IEnumerable<PolicyMethodBinding> GetInvariantMethods()
        {
            foreach (var typed in _typed)
            foreach (var method in typed.Value)
                yield return method;
            if (_indexed != null)
            {
                foreach (var indexed in _indexed)
                    foreach (var method in indexed.Value)
                        yield return method;
            }
        }

        internal IEnumerable<PolicyMethodBinding> GetInvariantMethods(object callback)
        {
            var key  = Policy.GetKey(callback);
            var type = key as Type;
            List<PolicyMethodBinding> methods = null;
            if (type != null)
                _typed.TryGetValue(type, out methods);
            else
                _indexed?.TryGetValue(key, out methods);
            return methods?.Where(method => method.Approves(callback))
                ?? Array.Empty<PolicyMethodBinding>();
        }

        internal IEnumerable<PolicyMethodBinding> GetCompatibleMethods(object callback)
        {
            var key = Policy.GetKey(callback);
            return _compatible.GetOrAdd(key, InferCompatibleMethods)
                .Where(method => method.Approves(callback));
        }

        private List<PolicyMethodBinding> InferCompatibleMethods(object key)
        {
            var compatible = new List<PolicyMethodBinding>();

            var type = key as Type;
            if (type != null)
            {
                var keys = Policy.GetCompatibleKeys(key, _typed.Keys);
                foreach (Type next in keys)
                {
                    List<PolicyMethodBinding> methods;
                    if (next != null && _typed.TryGetValue(next, out methods))
                        compatible.AddRange(methods);
                }
            }
            else if (_indexed != null)
            {
                var keys = Policy.GetCompatibleKeys(key, _indexed.Keys);
                foreach (var next in keys)
                {
                    List<PolicyMethodBinding> methods;
                    if (_indexed.TryGetValue(next, out methods))
                        compatible.AddRange(methods);
                }
            }

            compatible.Sort(Policy);

            if (_unknown != null)
                compatible.AddRange(_unknown);

            return compatible;
        }
    }
}