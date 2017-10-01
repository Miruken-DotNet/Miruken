namespace Miruken.Callback
{
    using System;
    using Concurrency;

    public static class HandlerBundleExtensions
    {
        public static bool Bundle(this IHandler handler,
                 Action<Bundle> prepare, bool all = true)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));
            var bundle = new Bundle(all);
            prepare(bundle);
            if (bundle.IsEmpty) return true;
            var handled  = handler.Handle(bundle);
            var complete = bundle.Complete();
            if (!bundle.WantsAsync && bundle.IsAsync)
                complete.Wait();
            return handled;
        }

        public static bool All(this IHandler handler, Action<Bundle> prepare)
        {
            return Bundle(handler, prepare);
        }

        public static bool Any(this IHandler handler, Action<Bundle> prepare)
        {
            return Bundle(handler, prepare, false);
        }

        public static Promise<bool> BundleAsync(this IHandler handler,
            Action<Bundle> prepare, bool all = true)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));
            var bundle = new Bundle(all) { WantsAsync = true };
            prepare(bundle);
            if (bundle.IsEmpty) return Promise.True;
            var handled = handler.Handle(bundle);
            return bundle.Complete().Then((r,s) => handled);
        }

        public static Promise<bool> AllAsync(this IHandler handler, Action<Bundle> prepare)
        {
            return BundleAsync(handler, prepare);
        }

        public static Promise<bool> AnyAsync(this IHandler handler, Action<Bundle> prepare)
        {
            return BundleAsync(handler, prepare, false);
        }
    }
}
