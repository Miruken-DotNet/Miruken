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
            var semantics = new CallbackSemantics();
            handler.Handle(semantics, true);
            var greedy  = semantics.HasOption(CallbackOptions.Broadcast);
            var handled = handler.Handle(batch, ref greedy);
            if (batch.IsAsync)
                batch.Complete().Wait();
            return handled || semantics.HasOption(CallbackOptions.BestEffort);
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
            var semantics = new CallbackSemantics();
            handler.Handle(semantics, true);
            var greedy    = semantics.HasOption(CallbackOptions.Broadcast);
            var handled   = handler.Handle(batch, ref greedy);
            return batch.Complete().Then((r,s) => handled ||
                semantics.HasOption(CallbackOptions.BestEffort));
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
