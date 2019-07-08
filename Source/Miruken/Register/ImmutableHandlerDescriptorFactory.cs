namespace Miruken.Register
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Infrastructure;
    using Microsoft.Extensions.DependencyInjection;

    public class ImmutableHandlerDescriptorFactory : AbstractHandlerDescriptorFactory
    {
        private readonly IDictionary<Type, HandlerDescriptor>
            Descriptors = new Dictionary<Type, HandlerDescriptor>();

        private readonly ConcurrentDictionary<Tuple<object, CallbackPolicy, bool, bool>,
            IEnumerable<HandlerDescriptor>> HandlerCache = 
            new ConcurrentDictionary<Tuple<object, CallbackPolicy, bool, bool>, IEnumerable<HandlerDescriptor>>();

        public ImmutableHandlerDescriptorFactory(
            IServiceCollection services,
            HandlerDescriptorVisitor visitor = null) : base(visitor)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            foreach (var descriptor in services)
            {
                var implementationType = GetImplementationType(descriptor);
                if (implementationType != null)
                {
                    var handler = CreateDescriptor(
                        implementationType, ServiceConfiguration.For(descriptor));
                    if (handler != null)
                        Descriptors.Add(implementationType, handler);
                }
            }
        }

        public override HandlerDescriptor GetDescriptor(Type type)
        {
            return Descriptors.TryGetValue(type, out var descriptor)
                ? descriptor
                : null;
        }

        public override HandlerDescriptor RegisterDescriptor(Type type,
            HandlerDescriptorVisitor visitor = null)
        {
            throw new NotSupportedException(
                "This factory is immutable and does not support ad-hoc registrations");
        }

        public override IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy)
        {
            return Descriptors.SelectMany(descriptor =>
            {
                CallbackPolicyDescriptor cpd = null;
                var handler = descriptor.Value;
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
            var key = Tuple.Create(policy.GetKey(callback), policy, instance, @static);
            return HandlerCache.GetOrAdd(key, k =>
                InternalGetCallbackHandlers(policy, callback, instance, @static));
        }

        private IEnumerable<HandlerDescriptor> InternalGetCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static)
        {
            if (Descriptors.Count == 0)
                return Enumerable.Empty<HandlerDescriptor>();

            List<PolicyMemberBinding> invariants = null;
            List<PolicyMemberBinding> compatible = null;

            foreach (var descriptor in Descriptors)
            {
                var handler = descriptor.Value;
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

        private static Type GetImplementationType(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationType != null)
                return descriptor.ImplementationType;

            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance.GetType();

            if (descriptor.ImplementationFactory != null)
            {
                var typeArguments = descriptor.ImplementationFactory
                    .GetType().GenericTypeArguments;
                if (typeArguments.Length == 2)
                    return typeArguments[1];
            }

            return null;
        }
    }
}
