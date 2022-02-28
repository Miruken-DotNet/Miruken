namespace Miruken.Callback;

using System.Linq;
using Policy;
using Policy.Bindings;

public sealed class StaticHandler : Handler, ICallbackPolicyDispatch
{
    bool ICallbackPolicyDispatch.Dispatch(
        CallbackPolicy policy, object callback, bool greedy,
        IHandler composer, ResultsDelegate results)
    {
        var handled  = false;
        var factory  = HandlerDescriptorFactory.Current;
        var handlers = factory.GetStaticHandlers(policy, callback);
        if (!greedy) handlers = handlers.Reverse();
        foreach (var descriptor in handlers)
        {
            if (!descriptor.Dispatch(
                    policy, descriptor.HandlerType, callback, greedy,
                    composer, results)) continue;
            if (!greedy) return true;
            handled = true;
        }
        return handled;
    }
}