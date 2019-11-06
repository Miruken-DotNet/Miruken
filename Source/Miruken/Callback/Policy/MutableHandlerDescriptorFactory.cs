namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Bindings;
    using Infrastructure;

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
                if (Descriptors.TryGetValue(definition, out descriptor))
                    return descriptor.Value.CloseDescriptor(type, RegisterDescriptor);
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
                var handler = descriptor.Value.Value;
                var staticMembers = handler.StaticPolicies?.TryGetValue(policy, out cpd) == true
                    ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                var members = handler.Policies?.TryGetValue(policy, out cpd) == true
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

            var descriptors = Descriptors.ToLookup(
                descriptor => descriptor.Value.Value.Priority != null);

            IEnumerable<PolicyMemberBinding> invariants        = null;
            IEnumerable<PolicyMemberBinding> compatible        = null;
            IEnumerable<PolicyMemberBinding> invariantsOrdered = null;
            IEnumerable<PolicyMemberBinding> compatibleOrdered = null;

            if (descriptors.Contains(false))
            {
                GetBindings(policy, descriptors[false],
                    callback, instance, @static, policy,
                    out invariants, out compatible);
            }

            if (descriptors.Contains(true))
            {
                GetBindings(policy, descriptors[true]
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

            var bindings = invariants == null ? compatible
                : compatible == null ? invariants
                : invariants.Concat(compatible);

            return bindings.Select(binding =>
            {
                var handler = binding.Dispatcher.Owner;
                if (handler.IsOpenGeneric)
                {
                    var key = policy.GetKey(callback);
                    return handler.CloseDescriptor(key, binding, RegisterDescriptor);
                }
                return handler;
            })
            .Where(descriptor => descriptor != null)
            .Distinct();
        }

        private static void GetBindings(CallbackPolicy policy, 
            IEnumerable<KeyValuePair<Type, Lazy<HandlerDescriptor>>> descriptors,
            object callback, bool instance, bool @static, 
            IComparer<PolicyMemberBinding> comparer, 
            out IEnumerable<PolicyMemberBinding> invariants,
            out IEnumerable<PolicyMemberBinding> compatible)
        {
            invariants = compatible = null;

            List<PolicyMemberBinding> invariantsList = null;
            List<PolicyMemberBinding> compatibleList = null;

            foreach (var descriptor in descriptors)
            {
                var handler = descriptor.Value.Value;
                CallbackPolicyDescriptor instanceCallbacks = null;
                if (instance)
                    handler.Policies?.TryGetValue(policy, out instanceCallbacks);

                var binding = instanceCallbacks?.GetInvariantMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (invariantsList == null)
                        invariants = invariantsList = new List<PolicyMemberBinding>();
                    invariantsList.Add(binding);
                    continue;
                }

                CallbackPolicyDescriptor staticCallbacks = null;
                if (@static)
                    handler.StaticPolicies?.TryGetValue(policy, out staticCallbacks);
                binding = staticCallbacks?.GetInvariantMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (invariantsList == null)
                        invariants = invariantsList = new List<PolicyMemberBinding>();
                    invariantsList.Add(binding);
                    continue;
                }

                binding = instanceCallbacks?.GetCompatibleMembers(callback).FirstOrDefault()
                       ?? staticCallbacks?.GetCompatibleMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (compatibleList == null)
                        compatible = compatibleList = new List<PolicyMemberBinding>();
                    if (comparer != null)
                        compatibleList.AddSorted(binding, comparer);
                    else
                        compatibleList.Add(binding);
                }
            }
        }
    }
}
