using System;
using Miruken.Callback;
using Miruken.Concurrency;

namespace Miruken.Context
{
    public static class ContextExtensions
    {
        public static ICallbackHandler TrackPromise(
            this ICallbackHandler handler, IContext context)
        {
            if (handler == null || context == null) return handler;
            return handler.Filter((callback, composer, proceed) =>
            {
                var handled = proceed();
                if (handled)
                {
                    var cb = callback as ICallback;
                    var promise = cb?.Result as Promise;
                    if (promise != null)
                    {
                        if (context.State == ContextState.Active)
                        {
                            Action<IContext> ended = ctx => promise.Cancel();
                            context.ContextEnded += ended;
                            promise.Finally(() => context.ContextEnded -= ended);
                        }
                        else
                            promise.Cancel();
                    }
                }
                return handled;
            });
        }
    }
}
