namespace Miruken.Callback
{
    using System;
    using System.Threading;
    using Concurrency;

    public class BatchDecorator : HandlerDecorator, IDisposable
    {
        private Promise<object[]>.ResolveCallbackT _resolved;
        private RejectCallback _rejected;
        private int _completed;
        
        public BatchDecorator(IHandler handler, params object[] tags)
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
            if (Batch != null && 
                (callback as IBatchCallback)?.AllowBatching != false)
            {
                var batch = Batch;
                if (_completed > 0 && !(callback is Composition)) {
                    Batch = null;
                }
                if (batch.Handle(callback, ref greedy, composer))
                    return true;
            }
            return base.HandleCallback(callback, ref greedy, composer);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
                this.Proxy<IBatchingComplete>().Complete(this)
                    .Then(_resolved, _rejected);
        }
    }

    public class NoBatchingDecorator : Handler, IDecorator
    {
        private readonly IHandler _handler;

        public NoBatchingDecorator(IHandler handler)
        {
            _handler = handler;
        }

        object IDecorator.Decoratee => _handler;

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            return _handler.Handle(new NoBatch(callback), ref greedy, composer);
        }
    }

    public static class HandlerBatchExtensions
    {
        public static BatchDecorator Batch(
            this IHandler handler, params object[] tags)
        {
            return handler != null 
                 ? new BatchDecorator(handler, tags) 
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

        public static IHandler NoBatch(this IHandler handler)
        {
            return handler == null ? null
                 : new NoBatchingDecorator(handler);    
        }

        public static Batch GetBatch(this IHandler handler, object tag = null)
        {
            var batch = handler?.Resolve<Batch>();
            if (batch == null) return null;
            return tag == null || batch.ShouldBatch(tag) ? batch : null;
        }
    }
}
