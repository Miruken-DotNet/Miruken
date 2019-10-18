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
                    return descriptor.Value.CloseDescriptor(type, this);
            }
            return null;
        }

        public override HandlerDescriptor RegisterDescriptor(Type type,
            HandlerDescriptorVisitor visitor = null)
        {
            try
            {
                var descriptor = Descriptors.GetOrAdd(type, t =>
                        new Lazy<HandlerDescriptor>(() => CreateDescriptor(t, visitor)))
                    .Value;
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

            List<PolicyMemberBinding> invariants = null;
            List<PolicyMemberBinding> compatible = null;

            foreach (var descriptor in Descriptors)
            {
                var handler = descriptor.Value.Value;
                CallbackPolicyDescriptor instanceCallbacks = null;
                if (instance)
                    handler.Policies?.TryGetValue(policy, out instanceCallbacks);

                var binding = instanceCallbacks?.GetInvariantMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (invariants == null)
                        invariants = new List<PolicyMemberBinding>();
                    invariants.Add(binding);
                    continue;
                }

                CallbackPolicyDescriptor staticCallbacks = null;
                if (@static)
                    handler.StaticPolicies?.TryGetValue(policy, out staticCallbacks);
                binding = staticCallbacks?.GetInvariantMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (invariants == null)
                        invariants = new List<PolicyMemberBinding>();
                    invariants.Add(binding);
                    continue;
                }

                binding = instanceCallbacks?.GetCompatibleMembers(callback).FirstOrDefault()
                       ?? staticCallbacks?.GetCompatibleMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (compatible == null)
                        compatible = new List<PolicyMemberBinding>();
                    compatible.AddSorted(binding, policy);
                }
            }

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
                    return handler.CloseDescriptor(key, binding, this);
                }
                return handler;
            })
            .Where(descriptor => descriptor != null)
            .Distinct();
        }
    }
}
