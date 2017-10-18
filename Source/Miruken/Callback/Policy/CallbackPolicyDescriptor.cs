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
            <object, List<Tuple<PolicyMethodBinding, int>>> _compatible;
        private Dictionary<object, List<PolicyMethodBinding>> _indexed;
        private List<PolicyMethodBinding> _unknown;

        public CallbackPolicyDescriptor(CallbackPolicy policy)
        {
            Policy      = policy;
            _typed      = new Dictionary<Type, List<PolicyMethodBinding>>();
            _compatible = new ConcurrentDictionary
                <object, List<Tuple<PolicyMethodBinding, int>>>();
        }

        public CallbackPolicy Policy { get; }

        internal void Add(PolicyMethodBinding method, Type handlerType)
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

        internal IEnumerable<PolicyMethodBinding> GetInvariantMethods(object key)
        {
            var type = key as Type;
            List<PolicyMethodBinding> methods = null;
            if (type != null)
                _typed.TryGetValue(type, out methods);
            else
                _indexed?.TryGetValue(key, out methods);
            return methods as ICollection<PolicyMethodBinding>
                ?? Array.Empty<PolicyMethodBinding>();
        }

        internal IEnumerable<Tuple<PolicyMethodBinding, int>> GetCompatibleMethods(object key)
        {
            return _compatible.GetOrAdd(key, InferCompatibleMethods);
        }

        internal List<Tuple<PolicyMethodBinding, int>> InferCompatibleMethods(object key)
        {
            var compatible = new List<Tuple<PolicyMethodBinding, int>>();

            var type = key as Type;
            if (type != null)
            {
                var keys = Policy.GetCompatibleKeys(key, _typed.Keys);
                foreach (var next in keys)
                {
                    List<PolicyMethodBinding> methods;
                    var nextType = next.Item1 as Type;
                    if (nextType != null && _typed.TryGetValue(nextType, out methods))
                        compatible.AddRange(methods.Select(m => Tuple.Create(m, next.Item2)));
                }
            }
            else if (_indexed != null)
            {
                var keys = Policy.GetCompatibleKeys(key, _indexed.Keys);
                foreach (var next in keys)
                {
                    List<PolicyMethodBinding> methods;
                    if (_indexed.TryGetValue(next.Item1, out methods))
                        compatible.AddRange(methods.Select(m => Tuple.Create(m, next.Item2)));
                }
            }

            if (_unknown != null)
                compatible.AddRange(_unknown.Select(u => Tuple.Create(u, int.MaxValue)));

            return compatible;
        }
    }
}