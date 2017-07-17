namespace Miruken.Callback
{
    using System;
    using Concurrency;

    public static class HandlerBatchExtensions
    {
        public static bool Batch(this IHandler handler,
                 Action<Batch> prepare, bool all = true)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));
            var batch = new Batch(all);
            prepare(batch);
            if (batch.IsEmpty) return true;
            var handled = handler.Handle(batch);
            if (batch.IsAsync)
                batch.Complete().Wait();
            return handled;
        }

        public static bool All(this IHandler handler, Action<Batch> prepare)
        {
            return Batch(handler, prepare);
        }

        public static bool Any(this IHandler handler, Action<Batch> prepare)
        {
            return Batch(handler, prepare, false);
        }

        public static Promise<bool> BatchAsync(this IHandler handler,
            Action<Batch> prepare, bool all = true)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));
            var batch = new Batch(all) { WantsAsync = true };
            prepare(batch);
            if (batch.IsEmpty) return Promise.True;
            var handled = handler.Handle(batch);
            return batch.Complete().Then((r,s) => handled);
        }

        public static Promise<bool> AllAsync(this IHandler handler, Action<Batch> prepare)
        {
            return BatchAsync(handler, prepare);
        }

        public static Promise<bool> AnyAsync(this IHandler handler, Action<Batch> prepare)
        {
            return BatchAsync(handler, prepare, false);
        }
    }
}
