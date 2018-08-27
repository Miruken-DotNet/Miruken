namespace Miruken.Callback
{
    using Policy;

    public sealed class StaticHandler : Handler, ICallbackPolicyDispatch
    {
        bool ICallbackPolicyDispatch.Dispatch(
            CallbackPolicy policy, object callback, bool greedy,
            IHandler composer, ResultsDelegate results)
        {
            var handled        = false;
            var staticHandlers = HandlerDescriptor.GetStaticHandlers(policy, callback);
            foreach (var handler in staticHandlers)
            {
                var descriptor = HandlerDescriptor.GetDescriptor(handler);
                if (descriptor.Dispatch(policy, handler, callback,
                                        greedy, composer, results))
                {
                    if (!greedy) return true;
                    handled = true;
                }
            }
            return handled;
        }
    }
}
