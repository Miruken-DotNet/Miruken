using Miruken.Callback;
using Miruken.Concurrency;

namespace Miruken.Context
{
    using System;

    public static class ContextExtensions
    {
        public static IHandler Async(this IHandler handler)
        {
            if (handler == null) return null;
            var context = handler as Context ?? handler.Resolve<Context>()
                       ?? throw new InvalidOperationException(
                              "Async support requires a Context");
            return handler.TrackPromise(context);
        }

        public static IHandler TrackPromise(
            this IHandler handler, Context context)
        {
            if (handler == null || context == null) return handler;
            return handler.Filter((callback, composer, proceed) =>
            {
                var handled = proceed();
                if (!handled) return false;
                var cb = callback as ICallback;
                if (!(cb?.Result is Promise promise)) return true;
                if (context.State == ContextState.Active)
                {
                    void Ended(Context ctx, object reason) => promise.Cancel();
                    context.ContextEnded += Ended;
                    promise.Finally(() => context.ContextEnded -= Ended);
                }
                else
                    promise.Cancel();
                return true;
            });
        }

        public static IHandler Dispose(
            this IHandler handler, IDisposable disposable)
        {
            if (handler == null || disposable == null) return handler;
            var context = handler as Context ?? handler.Resolve<Context>()
                        ?? throw new InvalidOperationException(
                               "Disposal support requires a Context");
            context.ContextEnded += (ctx, _) =>
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // don't care
                }
            };
            return handler;
        }

        public static IHandler PublishFromRoot(this IHandler handler)
        {
            var context = handler.Resolve<Context>()
                ?? throw new InvalidOperationException(
                              "he root context could not be found");
            return context.Root.Publish();
        }
    }
}
