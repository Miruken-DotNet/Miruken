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

        IEnumerable<Type> GetStaticHandlers(CallbackPolicy policy, object callback);

        IEnumerable<Type> GetInstanceHandlers(CallbackPolicy policy, object callback);

        IEnumerable<Type> GetCallbackHandlers(CallbackPolicy policy, object callback);

        IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy);
    }
}
