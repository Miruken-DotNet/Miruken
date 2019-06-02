namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using Bindings;

    public delegate void HandlerDescriptorVisitor(
        HandlerDescriptor descriptor, PolicyMemberBinding binding);

    public interface IHandlerDescriptorFactory
    {
        HandlerDescriptor GetDescriptor(Type type);

        HandlerDescriptor RegisterDescriptor(Type type, HandlerDescriptorVisitor visitor = null);

        IEnumerable<HandlerDescriptor> GetStaticHandlers(CallbackPolicy policy, object callback);

        IEnumerable<HandlerDescriptor> GetInstanceHandlers(CallbackPolicy policy, object callback);

        IEnumerable<HandlerDescriptor> GetCallbackHandlers(CallbackPolicy policy, object callback);

        IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy);
    }
}
