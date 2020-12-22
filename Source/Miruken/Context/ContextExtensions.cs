using Miruken.Callback;
using Miruken.Concurrency;

namespace Miruken.Context
{
    using System;
    using System.Linq;

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

        public static IHandler TrackPromise(this IHandler handler, Context context)
        {
            if (handler == null || context == null) return handler;
            return handler.Filter((callback, _, proceed) =>
            {
                var handled = proceed();
                if (!handled) return false;
                var cb = callback as ICallback;
                if (!(cb?.Result is Promise promise)) return true;
                context.Track(promise);
                return true;
            });
        }

        public static IHandler Track(this IHandler handler, Promise promise)
        {
            if (promise == null)
                throw new ArgumentNullException(nameof(promise));
            var context = handler as Context ?? handler.Resolve<Context>()
                ?? throw new InvalidOperationException(
                              "Tracking support requires a Context");
            if (context.State == ContextState.Active)
                promise.Finally(() => context.ContextEnded += (_, _) => promise.Cancel());
            else
                promise.Cancel();
            return handler;
        }

        public static IHandler Dispose(this IHandler handler, IDisposable disposable)
        {
            if (handler == null || disposable == null) return handler;
            var context = handler as Context ?? handler.Resolve<Context>()
                        ?? throw new InvalidOperationException(
                               "Disposal support requires a Context");
            context.ContextEnded += (_, _) =>
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

        public static Context Parent(this Context context, int howMany)
        {
            while (true)
            {
                if (context == null || howMany < 0) return null;
                if (howMany == 0) return context;
                context = context.Parent;
                howMany -= 1;
            }
        }

        public static Context Deepest(this Context context)
        {
            while (true)
            {
                if (context.HasChildren)
                {
                    context = context.Children.Last();
                    continue;
                }
                return context;
            }
        }
    }
}
