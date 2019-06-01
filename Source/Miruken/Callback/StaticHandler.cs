namespace Miruken.Callback
{
    using Policy;
    using Policy.Bindings;

    public sealed class StaticHandler : Handler, ICallbackPolicyDispatch
    {
        bool ICallbackPolicyDispatch.Dispatch(
            CallbackPolicy policy, object callback, bool greedy,
            IHandler composer, ResultsDelegate results)
        {
            var handled        = false;
            var factory        = HandlerDescriptorFactory.Current;
            var staticHandlers = factory.GetStaticHandlers(policy, callback);
            foreach (var descriptor in staticHandlers)
            {
                if (descriptor.Dispatch(
                        policy, descriptor.HandlerType, callback, greedy,
                        composer, results))
                {
                    if (!greedy) return true;
                    handled = true;
                }
            }
            return handled;
        }
    }
}
