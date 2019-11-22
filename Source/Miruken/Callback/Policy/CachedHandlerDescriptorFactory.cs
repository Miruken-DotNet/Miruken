namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Bindings;

    public class CachedHandlerDescriptorFactory : IHandlerDescriptorFactory
    {
        private readonly IHandlerDescriptorFactory _factory;
        private readonly ConcurrentDictionary<Tuple<bool, bool, object>, IEnumerable<HandlerDescriptor>> _descriptors;
        private readonly ConcurrentDictionary<CallbackPolicy, IEnumerable<PolicyMemberBinding>> _bindings;

        public CachedHandlerDescriptorFactory(IHandlerDescriptorFactory factory)
        {
            _factory     = factory;
            _descriptors = new ConcurrentDictionary<Tuple<bool, bool, object>, IEnumerable<HandlerDescriptor>>();
            _bindings    = new ConcurrentDictionary<CallbackPolicy, IEnumerable<PolicyMemberBinding>>();
        }

        public HandlerDescriptor GetDescriptor(Type type)
        {
            return _factory.GetDescriptor(type);
        }

        public HandlerDescriptor RegisterDescriptor(
            Type type, HandlerDescriptorVisitor visitor = null, int? priority = null)
        {
            var descriptor = _factory.RegisterDescriptor(type, visitor, priority);
            _descriptors.Clear();
            _bindings.Clear();
            return descriptor;
        }

        public IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy)
        {
            return _bindings.GetOrAdd(policy, p => _factory.GetPolicyMembers(p));
        }

        public IEnumerable<HandlerDescriptor> GetInstanceHandlers(CallbackPolicy policy, object callback)
        {
            var key = policy.GetKey(callback);
            return key == null
                 ? _factory.GetInstanceHandlers(policy, callback)
                 : _descriptors.GetOrAdd(Tuple.Create(true, false, key),
                     k => _factory.GetInstanceHandlers(policy, callback));
        }

        public IEnumerable<HandlerDescriptor> GetStaticHandlers(CallbackPolicy policy, object callback)
        {
            var key = policy.GetKey(callback);
            return key == null
                ? _factory.GetStaticHandlers(policy, callback)
                : _descriptors.GetOrAdd(Tuple.Create(false, true, key),
                    k => _factory.GetStaticHandlers(policy, callback));
        }

        public IEnumerable<HandlerDescriptor> GetCallbackHandlers(CallbackPolicy policy, object callback)
        {
            var key = policy.GetKey(callback);
            return key == null
                ? _factory.GetCallbackHandlers(policy, callback)
                : _descriptors.GetOrAdd(Tuple.Create(true, true, key),
                    k => _factory.GetCallbackHandlers(policy, callback));
        }
    }
}
