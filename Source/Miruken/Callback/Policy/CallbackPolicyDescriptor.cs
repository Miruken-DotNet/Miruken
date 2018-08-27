namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public class CallbackPolicyDescriptor
    {
        private readonly Dictionary<Type, List<PolicyMemberBinding>> _typed;
        private readonly ConcurrentDictionary
            <object, List<PolicyMemberBinding>> _compatible;
        private Dictionary<object, List<PolicyMemberBinding>> _indexed;
        private List<PolicyMemberBinding> _unknown;

        public CallbackPolicyDescriptor(CallbackPolicy policy)
        {
            Policy      = policy;
            _typed      = new Dictionary<Type, List<PolicyMemberBinding>>();
            _compatible = new ConcurrentDictionary
                <object, List<PolicyMemberBinding>>();
        }

        public CallbackPolicy Policy { get; }

        internal void Add(PolicyMemberBinding member)
        {
            var key = member.Key;
            if (key == null)
            {
                var unknown = _unknown ??
                    (_unknown = new List<PolicyMemberBinding>());
                unknown.Add(member);
                return;
            }

            List<PolicyMemberBinding> methods;

            if (key is Type type)
            {
                if (!_typed.TryGetValue(type, out methods))
                {
                    methods = new List<PolicyMemberBinding>();
                    _typed.Add(type, methods);
                }
            }
            else
            {
                var indexed = _indexed ?? 
                    (_indexed = new Dictionary<object, List<PolicyMemberBinding>>());
                if (!indexed.TryGetValue(key, out methods))
                {
                    methods = new List<PolicyMemberBinding>();
                    indexed.Add(key, methods);
                }
            }

            methods.Add(member);
        }

        internal IEnumerable<PolicyMemberBinding> GetInvariantMethods()
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

        internal IEnumerable<PolicyMemberBinding> GetInvariantMethods(object callback)
        {
            var key = Policy.GetKey(callback);
            List<PolicyMemberBinding> methods = null;
            if (key is Type type)
                _typed.TryGetValue(type, out methods);
            else
                _indexed?.TryGetValue(key, out methods);
            return methods?.Where(method => method.Approves(callback))
                ?? Array.Empty<PolicyMemberBinding>();
        }

        internal IEnumerable<PolicyMemberBinding> GetCompatibleMethods(object callback)
        {
            var key = Policy.GetKey(callback);
            return _compatible.GetOrAdd(key, InferCompatibleMethods)
                .Where(method => method.Approves(callback));
        }

        private List<PolicyMemberBinding> InferCompatibleMethods(object key)
        {
            var compatible = new List<PolicyMemberBinding>();

            if (key is Type)
            {
                var keys = Policy.GetCompatibleKeys(key, _typed.Keys);
                foreach (Type next in keys)
                {
                    if (next != null && _typed.TryGetValue(next, out var methods))
                        compatible.AddRange(methods);
                }
            }
            else if (_indexed != null)
            {
                var keys = Policy.GetCompatibleKeys(key, _indexed.Keys);
                foreach (var next in keys)
                {
                    if (_indexed.TryGetValue(next, out var methods))
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