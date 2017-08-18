namespace Miruken.Callback
{
    using System;
    using System.Threading;
    using Concurrency;

    public class HandlerBatch : HandlerDecorator, IDisposable
    {
        private Promise<object[]>.ResolveCallbackT _resolved;
        private RejectCallback _rejected;
        private int _completed;
        
        public HandlerBatch(IHandler handler, params object[] tags)
            : base(handler)
        {
            Batch     = new Batch(tags);
            Completed = new Promise<object[]>((resolved, rejected) =>
            {
                _resolved = resolved;
                _rejected = rejected;
            });
        }

        [Provides]
        public Batch Batch { get; private set; }

        public Promise<object[]> Completed { get; }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            var handled = false;
            if (Batch != null && 
                (callback as IBatchCallback)?.AllowBatching != false)
            {
                var batch = Batch;
                if (_completed > 0 && !(callback is Composition)) {
                    Batch = null;
                }
                if ((handled = batch.Handle(callback, ref greedy, composer)) 
                    && !greedy) return true;
            }
            return base.HandleCallback(callback, ref greedy, composer)
                || handled;
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
                this.Proxy<IBatchingComplete>().Complete(this)
                    .Then(_resolved, _rejected);
        }
    }

    public static class HandlerBatchExtensions
    {
        public static HandlerBatch Batch(
            this IHandler handler, params object[] tags)
        {
            return handler != null 
                 ? new HandlerBatch(handler, tags) 
                 : null;
        }

        public static Promise<object[]> Batch(
            this IHandler handler, Action<IHandler> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            using (var batch = Batch(handler))
            {
                if (batch == null)
                    return Promise<object[]>.Empty;
                configure(batch);
                return batch.Completed;
            }
        }

        public static Promise<object[]> Batch(this IHandler handler,
            object[] tags, Action<IHandler> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            using (var batch = Batch(handler, tags))
            {
                if (batch == null)
                    return Promise<object[]>.Empty;
                configure(batch);
                return batch.Completed;
            }
        }

        public static Batch GetBatch(this IHandler handler, object tag = null)
        {
            var batch = handler?.Resolve<Batch>();
            if (batch == null) return null;
            return tag == null || batch.ShouldBatch(tag) ? batch : null;
        }
    }
}
