namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Bindings;

    public class MutableHandlerDescriptorFactory : AbstractHandlerDescriptorFactory
    {
        private readonly ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>
            Descriptors = new ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>();

        public MutableHandlerDescriptorFactory(HandlerDescriptorVisitor visitor = null)
            : base(visitor)
        {
            ImplicitLifestyle = new SingletonAttribute();
        }

        public override HandlerDescriptor GetDescriptor(Type type)
        {
            if (Descriptors.TryGetValue(type, out var descriptor))
                return descriptor.Value;
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var definition = type.GetGenericTypeDefinition();
                if (Descriptors.TryGetValue(definition, out descriptor) &&
                    descriptor.Value is GenericHandlerDescriptor genericDescriptor)
                    return genericDescriptor.CloseDescriptor(type, CreateDescriptor);
            }
            return null;
        }

        public override HandlerDescriptor RegisterDescriptor(Type type,
            HandlerDescriptorVisitor visitor = null, int? priority = null)
        {
            try
            {
                var descriptor = Descriptors.GetOrAdd(type,
                        t => new Lazy<HandlerDescriptor>(
                            () => CreateDescriptor(t, visitor, priority))).Value;
                if (descriptor == null)
                    Descriptors.TryRemove(type, out _);
                return descriptor;
            }
            catch
            {
                Descriptors.TryRemove(type, out _);
                throw;
            }
        }

        public override IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy)
        {
            return Descriptors.SelectMany(descriptor =>
            {
                CallbackPolicyDescriptor cpd = null;
                var handler       = descriptor.Value.Value;
                var staticMembers = handler.StaticPolicies?.TryGetValue(policy, out cpd) == true
                    ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                var members       = handler.Policies?.TryGetValue(policy, out cpd) == true
                    ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                if (staticMembers == null) return members;
                return members == null ? staticMembers : staticMembers.Concat(members);
            });
        }

        protected override IEnumerable<HandlerDescriptor> GetCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static)
        {
            if (Descriptors.Count == 0)
                return Enumerable.Empty<HandlerDescriptor>();

            var lookup = Descriptors.ToLookup(
                descriptor => descriptor.Value.Value.Priority != null);

            IEnumerable<HandlerDescriptor> invariants        = null;
            IEnumerable<HandlerDescriptor> compatible        = null;
            IEnumerable<HandlerDescriptor> invariantsOrdered = null;
            IEnumerable<HandlerDescriptor> compatibleOrdered = null;

            if (lookup.Contains(false))
            {
                GetDescriptors(policy, lookup[false],
                    callback, instance, @static, policy,
                    out invariants, out compatible);
            }

            if (lookup.Contains(true))
            {
                GetDescriptors(policy, lookup[true]
                        .OrderBy(d => d.Value.Value.Priority),
                    callback, instance, @static, null,
                    out invariantsOrdered, out compatibleOrdered);
            }

            if (invariants == null)
                invariants = invariantsOrdered;
            else if (invariantsOrdered != null)
                invariants = invariants.Concat(invariantsOrdered);

            if (compatible == null)
                compatible = compatibleOrdered;
            else if (compatibleOrdered != null)
                compatible = compatible.Concat(compatibleOrdered);

            if (invariants == null && compatible == null)
                return Enumerable.Empty<HandlerDescriptor>();

            return invariants == null ? compatible
                : compatible == null ? invariants
                : invariants.Concat(compatible);
        }

        private void GetDescriptors(CallbackPolicy policy, 
            IEnumerable<KeyValuePair<Type, Lazy<HandlerDescriptor>>> descriptors,
            object callback, bool instance, bool @static, 
            IComparer<PolicyMemberBinding> orderBy, 
            out IEnumerable<HandlerDescriptor> invariants,
            out IEnumerable<HandlerDescriptor> compatible)
        {
            invariants = compatible = null;

            object key = null;
            List<HandlerDescriptor> invariantsList = null;
            List<HandlerDescriptor> compatibleList = null;
            SortedDictionary<PolicyMemberBinding, HandlerDescriptor> sortedCompatibleList = null;

            foreach (var descriptor in descriptors)
            {
                var handler = descriptor.Value.Value;
                CallbackPolicyDescriptor instanceCallbacks = null;
                if (instance)
                    handler.Policies?.TryGetValue(policy, out instanceCallbacks);

                if (instanceCallbacks?.GetInvariantMembers(callback).Any() == true)
                {
                    if (invariantsList == null)
                        invariants = invariantsList = new List<HandlerDescriptor>();
                    invariantsList.Add(handler);
                    continue;
                }

                CallbackPolicyDescriptor staticCallbacks = null;
                if (@static)
                    handler.StaticPolicies?.TryGetValue(policy, out staticCallbacks);
                if (staticCallbacks?.GetInvariantMembers(callback).Any() == true)
                {
                    if (invariantsList == null)
                        invariants = invariantsList = new List<HandlerDescriptor>();
                    invariantsList.Add(handler);
                    continue;
                }

                var binding =
                    instanceCallbacks?.GetCompatibleMembers(callback).FirstOrDefault()
                    ?? staticCallbacks?.GetCompatibleMembers(callback).FirstOrDefault();

                if (binding != null)
                {
                    if (handler is GenericHandlerDescriptor genericHandler)
                    {
                        if (key == null)
                            key = policy.GetKey(callback);
                        handler = genericHandler.CloseDescriptor(key, binding, CreateDescriptor);
                        if (handler == null) continue;
                    }

                    if (orderBy != null)
                    {
                        if (sortedCompatibleList == null)
                            sortedCompatibleList = new SortedDictionary<PolicyMemberBinding, HandlerDescriptor>(
                                new DuplicateComparer(orderBy));
                        sortedCompatibleList.Add(binding, handler);
                    }
                    else
                    {
                        if (compatibleList == null)
                            compatible = compatibleList = new List<HandlerDescriptor>();
                        compatibleList.Add(handler);
                    }
                }
            }

            if (sortedCompatibleList != null)
                compatible = sortedCompatibleList.Values.Reverse();
        }

        private class DuplicateComparer : IComparer<PolicyMemberBinding>
        {
            private readonly IComparer<PolicyMemberBinding> _comparer;

            public DuplicateComparer(IComparer<PolicyMemberBinding> comparer)
            {
                _comparer = comparer;
            }

            public int Compare(PolicyMemberBinding x, PolicyMemberBinding y)
            {
                var result = _comparer.Compare(x, y);
                return result == 0 ? -1 : result;
            }
        }
    }
}
