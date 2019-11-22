using System;
using System.Collections.Generic;
using System.Text;

namespace Miruken.Callback.Policy
{
    using Bindings;

    public class CachedHandlerDescriptorFactory : IHandlerDescriptorFactory
    {
        private readonly IHandlerDescriptorFactory _factory;

        public CachedHandlerDescriptorFactory(IHandlerDescriptorFactory factory)
        {
            _factory = factory;
        }
        public HandlerDescriptor GetDescriptor(Type type)
        {
            return _factory.GetDescriptor(type);
        }

        public HandlerDescriptor RegisterDescriptor(
            Type type, HandlerDescriptorVisitor visitor = null, int? priority = null)
        {
            return _factory.RegisterDescriptor(type, visitor, priority);
        }

        public IEnumerable<HandlerDescriptor> GetStaticHandlers(CallbackPolicy policy, object callback)
        {
            return _factory.GetStaticHandlers(policy, callback);
        }

        public IEnumerable<HandlerDescriptor> GetInstanceHandlers(CallbackPolicy policy, object callback)
        {
            return _factory.GetInstanceHandlers(policy, callback);
        }

        public IEnumerable<HandlerDescriptor> GetCallbackHandlers(CallbackPolicy policy, object callback)
        {
            return _factory.GetCallbackHandlers(policy, callback);
        }

        public IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy)
        {
            return _factory.GetPolicyMembers(policy);
        }
    }
}
