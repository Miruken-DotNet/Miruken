namespace Miruken.Callback
{
    using System;
    using Concurrency;

    public static class HandlerBatchExtensions
    {
        public static Promise Batch(this IHandler handler,
            Action<Batch> prepare, bool all = true)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));
            var batch = new Batch(all);
            prepare(batch);
            var semantics = new CallbackSemantics();
            handler.Handle(semantics, true);
            var greedy = semantics.HasOption(CallbackOptions.Broadcast);
            if (!handler.Handle(batch, ref greedy))
            {
                if (!semantics.HasOption(CallbackOptions.BestEffort))
                    return Promise.Rejected(new IncompleteBatchException());

            }
            return batch.Complete();
        }

        public static Promise All(this IHandler handler, Action<Batch> prepare)
        {
            return Batch(handler, prepare);
        }

        public static Promise Any(this IHandler handler, Action<Batch> prepare)
        {
            return Batch(handler, prepare, false);
        }
    }

    public class IncompleteBatchException : Exception { }
}
