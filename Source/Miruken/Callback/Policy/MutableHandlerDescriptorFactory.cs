namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Bindings;
    using Infrastructure;

    public class MutableHandlerDescriptorFactory : AbstractHandlerDescriptorFactory
    {
        private readonly List<HandlerDescriptor> _orderedDescriptors;
        private readonly Dictionary<Type, HandlerDescriptor> _descriptors;
        private readonly ReaderWriterLock _lock;

        public MutableHandlerDescriptorFactory(HandlerDescriptorVisitor visitor = null)
            : base(visitor)
        {
            _lock               = new ReaderWriterLock();
            _orderedDescriptors = new List<HandlerDescriptor>();
            _descriptors        = new Dictionary<Type, HandlerDescriptor>();
            ImplicitLifestyle   = new SingletonAttribute();
        }

        public override HandlerDescriptor GetDescriptor(Type type)
        {
            _lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (_descriptors.TryGetValue(type, out var descriptor))
                    return descriptor;
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var definition = type.GetGenericTypeDefinition();
                    if (_descriptors.TryGetValue(definition, out descriptor))
                    {
                        var cookie = _lock.UpgradeToWriterLock(Timeout.Infinite);
                        try
                        {
                            return descriptor.CloseDescriptor(type, CreateAndRegisterDescriptor);
                        }
                        finally
                        {
                            _lock.DowngradeFromWriterLock(ref cookie);
                        }
                    }
                }
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
            return null;
        }

        public override HandlerDescriptor RegisterDescriptor(Type type,
            HandlerDescriptorVisitor visitor = null)
        {
            _lock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                return _descriptors.TryGetValue(type, out var descriptor)
                     ? descriptor : CreateAndRegisterDescriptor(type, visitor);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public override IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy)
        {
            _lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _orderedDescriptors.SelectMany(descriptor =>
                {
                    CallbackPolicyDescriptor cpd = null;
                    var staticMembers = descriptor.StaticPolicies?.TryGetValue(policy, out cpd) == true
                        ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                    var members = descriptor.Policies?.TryGetValue(policy, out cpd) == true
                        ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                    if (staticMembers == null) return members;
                    return members == null ? staticMembers : staticMembers.Concat(members);
                });
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        protected override IEnumerable<HandlerDescriptor> GetCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static)
        {
            _lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (_orderedDescriptors.Count == 0)
                    return Enumerable.Empty<HandlerDescriptor>();

                List<PolicyMemberBinding> invariants = null;
                List<PolicyMemberBinding> compatible = null;

                foreach (var descriptor in _orderedDescriptors)
                {
                    CallbackPolicyDescriptor instanceCallbacks = null;
                    if (instance)
                        descriptor.Policies?.TryGetValue(policy, out instanceCallbacks);

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
                        descriptor.StaticPolicies?.TryGetValue(policy, out staticCallbacks);
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
                        return handler.CloseDescriptor(key, binding, (t, v) =>
                        {
                            if (_descriptors.TryGetValue(t, out var descriptor))
                                return descriptor;
                            var cookie = _lock.UpgradeToWriterLock(Timeout.Infinite);
                            try
                            {
                                return CreateAndRegisterDescriptor(t, v);
                            }
                            finally
                            {
                                _lock.DowngradeFromWriterLock(ref cookie);
                            }
                        });
                    }
                    return handler;
                })
                .Where(descriptor => descriptor != null)
                .Distinct();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        private HandlerDescriptor CreateAndRegisterDescriptor(Type type,
            HandlerDescriptorVisitor visitor)
        {
            var descriptor = CreateDescriptor(type, visitor);
            if (descriptor != null)
            {
                _descriptors.Add(type, descriptor);
                _orderedDescriptors.Add(descriptor);
                return descriptor;
            }
            return null;
        }
    }
}
