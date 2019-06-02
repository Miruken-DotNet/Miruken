namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bindings;
    using Infrastructure;

    public static class HandlerDescriptorFactory
    {
        private static IHandlerDescriptorFactory _factory;

        public static HandlerDescriptor GetDescriptor<T>(
            this IHandlerDescriptorFactory factory)
        {
            return factory.GetDescriptor(typeof(T));
        }

        public static HandlerDescriptor RegisterDescriptor<T>(
            this IHandlerDescriptorFactory factory,
            HandlerDescriptorVisitor visitor = null)
        {
            return factory.RegisterDescriptor(typeof(T), visitor);
        }

        public static IEnumerable<PolicyMemberBinding> GetPolicyMembers<T>(
            this IHandlerDescriptorFactory factory, CallbackPolicy policy)
        {
            return factory.GetPolicyMembers(policy)
                .Where(m => (m.Key as Type)?.Is<T>() == true);
        }

        public static IHandlerDescriptorFactory Current =>
            _factory ?? MutableHandlerDescriptorFactory.Default;

        public static void UseFactory(IHandlerDescriptorFactory factory)
        {
            _factory = factory;
        }
    }
}