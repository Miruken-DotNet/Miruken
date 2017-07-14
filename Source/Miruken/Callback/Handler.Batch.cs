namespace Miruken.Callback
{
    using System;
    using Concurrency;

    public static class HandlerBatchExtensions
    {
        public static void Batch(this IHandler handler,
                 Action<Batch> prepare, bool all = true)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));
            var batch = new Batch(all);
            prepare(batch);
            if (batch.IsEmpty) return;
            var semantics = new CallbackSemantics();
            handler.Handle(semantics, true);
            var greedy = semantics.HasOption(CallbackOptions.Broadcast);
            var handled = handler.Handle(batch, ref greedy);
            if (batch.IsAsync)
                batch.Complete().Wait();
            if (!(handled || semantics.HasOption(CallbackOptions.BestEffort)))
                throw new IncompleteBatchException();
        }

        public static void All(this IHandler handler, Action<Batch> prepare)
        {
            Batch(handler, prepare);
        }

        public static void Any(this IHandler handler, Action<Batch> prepare)
        {
            Batch(handler, prepare, false);
        }

        public static Promise BatchAsync(this IHandler handler,
            Action<Batch> prepare, bool all = true)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));
            var batch = new Batch(all) { WantsAsync = true };
            prepare(batch);
            if (batch.IsEmpty)
                return Promise.Empty;
            var semantics = new CallbackSemantics();
            handler.Handle(semantics, true);
            var greedy    = semantics.HasOption(CallbackOptions.Broadcast);
            var handled   = handler.Handle(batch, ref greedy);
            var complete  = batch.Complete();
            if (!(handled || semantics.HasOption(CallbackOptions.BestEffort)))
                complete = complete.Then((r,s) => Promise.Rejected(
                    new IncompleteBatchException()));
            return complete;
        }

        public static Promise AllAsync(this IHandler handler, Action<Batch> prepare)
        {
            return BatchAsync(handler, prepare);
        }

        public static Promise AnyAsync(this IHandler handler, Action<Batch> prepare)
        {
            return BatchAsync(handler, prepare, false);
        }
    }

    public class IncompleteBatchException : Exception { }
}
