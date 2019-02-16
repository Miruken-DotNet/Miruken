namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Bindings;
    using Infrastructure;

    public class CallbackPolicyDescriptor
    {
        private readonly Dictionary<Type, List<PolicyMemberBinding>> _typed;
        private readonly ConcurrentDictionary<object, List<PolicyMemberBinding>> _compatible;
        private Dictionary<object, List<PolicyMemberBinding>> _indexed;
        private List<PolicyMemberBinding> _unknown;
        private IEnumerable<PolicyMemberBinding> _invariant;

        public CallbackPolicyDescriptor(CallbackPolicy policy)
        {
            Policy      = policy;
            _typed      = new Dictionary<Type, List<PolicyMemberBinding>>();
            _compatible = new ConcurrentDictionary <object, List<PolicyMemberBinding>>();
        }

        public CallbackPolicy Policy { get; }

        public IEnumerable<PolicyMemberBinding> InvariantMembers =>
            LazyInitializer.EnsureInitialized(ref _invariant, GetInvariantMembers);

        internal void Add(PolicyMemberBinding member)
        {
            var key = member.Key;
            if (key == null)
            {
                var unknown = _unknown ??
                    (_unknown = new List<PolicyMemberBinding>());
                unknown.AddSorted(member,
                    PolicyMemberBinding.OrderByArity);
                return;
            }

            List<PolicyMemberBinding> members;

            if (key is Type type)
            {
                if (!_typed.TryGetValue(type, out members))
                {
                    members = new List<PolicyMemberBinding>();
                    _typed.Add(type, members);
                }
            }
            else
            {
                var indexed = _indexed ?? 
                    (_indexed = new Dictionary<object, List<PolicyMemberBinding>>());
                if (!indexed.TryGetValue(key, out members))
                {
                    members = new List<PolicyMemberBinding>();
                    indexed.Add(key, members);
                }
            }

            members.AddSorted(member, PolicyMemberBinding.OrderByArity);
        }

        private IEnumerable<PolicyMemberBinding> GetInvariantMembers()
        {
            foreach (var typed in _typed)
            foreach (var member in typed.Value)
                yield return member;
            if (_indexed != null)
            {
                foreach (var indexed in _indexed)
                    foreach (var member in indexed.Value)
                        yield return member;
            }
        }

        public IEnumerable<PolicyMemberBinding> GetInvariantMembers(object callback)
        {
            var key = Policy.GetKey(callback);
            List<PolicyMemberBinding> members = null;
            if (key is Type type)
                _typed.TryGetValue(type, out members);
            else
                _indexed?.TryGetValue(key, out members);
            return members?.Where(member => member.Approves(callback))
                ?? Array.Empty<PolicyMemberBinding>();
        }

        public IEnumerable<PolicyMemberBinding> GetCompatibleMembers(object callback)
        {
            var key = Policy.GetKey(callback);
            return _compatible.GetOrAdd(key, InferCompatibleMembers)
                .Where(member => member.Approves(callback));
        }

        private List<PolicyMemberBinding> InferCompatibleMembers(object key)
        {
            var compatible = new List<PolicyMemberBinding>();

            if (key is Type)
            {
                var keys = Policy.GetCompatibleKeys(key, _typed.Keys);
                foreach (Type next in keys)
                {
                    if (next != null && _typed.TryGetValue(next, out var members))
                        compatible.AddRange(members);
                }
            }
            else if (_indexed != null)
            {
                var keys = Policy.GetCompatibleKeys(key, _indexed.Keys);
                foreach (var next in keys)
                {
                    if (_indexed.TryGetValue(next, out var members))
                        compatible.AddRange(members);
                }
            }

            compatible.Sort(Policy);

            if (_unknown != null)
                compatible.AddRange(_unknown);

            return compatible;
        }
    }
}