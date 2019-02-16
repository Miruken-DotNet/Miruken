namespace Miruken.Callback
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Concurrency;

    public class BatchHandler : DecoratedHandler
    {
        private int _completed;
        
        public BatchHandler(
            IHandler handler, params object[] tags)
            : base(handler)
        {
            Batch = new Batch(tags);
        }

        [Provides]
        public Batch Batch { get; private set; }

        [Provides]
        public TBatch GetBatcher<TBatch>()
            where TBatch : class, IBatching, new()
        {
            if (Batch != null)
            {
                var batcher = Batch.FindHandler<TBatch>();
                if (batcher == null)
                    Batch.AddHandlers(batcher = new TBatch());
                return batcher;
            }
            return null;
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            if (Batch != null && 
                (callback as IBatchCallback)?.CanBatch != false)
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

        public Promise<object[]> Complete(Promise complete = null)
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) == 1)
                throw new InvalidOperationException("The batch has already completed");
            var results = this.Proxy<IBatchingComplete>().Complete(this);
            return complete != null
                 ? results.Then((r, s) => complete.Then((_, ss) => r))
                 : results;
        }
    }

    public sealed class NoBatchDecorator : Handler, IDecorator
    {
        private readonly IHandler _handler;

        public NoBatchDecorator(IHandler handler)
        {
            _handler = handler;
        }

        object IDecorator.Decoratee => _handler;

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            var inquiry = ((callback as Composition)?.Callback
                       ?? callback) as Inquiry;
            return inquiry?.Key as Type != typeof(Batch) &&
                   _handler.Handle(new NoBatch(callback), ref greedy, composer);
        }
    }

    public static class HandlerBatchExtensions
    {
        public static BatchHandler Batch(
            this IHandler handler, params object[] tags)
        {
            return handler != null 
                 ? new BatchHandler(handler, tags) 
                 : null;
        }

        public static Promise<object[]> Batch(
            this IHandler handler, Action<IHandler> configure)
        {
            return handler.Batch(null, configure);
        }

        public static Promise<object[]> Batch(
            this IHandler handler, Func<IHandler, Task> configure)
        {
            return handler.Batch(null, configure);
        }

        public static Promise<object[]> Batch(this IHandler handler,
            object[] tags, Action<IHandler> configure)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            var batch = new BatchHandler(handler, tags);
            configure(batch);
            return batch.Complete();
        }

        public static Promise<object[]> Batch(this IHandler handler,
            object[] tags, Func<IHandler, Task> configure)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            var batch = new BatchHandler(handler, tags);
            return batch.Complete(configure(batch));
        }

        public static Promise<object[]> Batch<TTag>(this IHandler handler,
            Action<IHandler> configure)
        {
            return handler.Batch(new [] { typeof(TTag) }, configure);
        }

        public static Promise<object[]> Batch<TTag>(this IHandler handler,
            Func<IHandler, Task> configure)
        {
            return handler.Batch(new [] { typeof(TTag) }, configure);
        }

        public static IHandler NoBatch(this IHandler handler)
        {
            return handler == null ? null
                 : new NoBatchDecorator(handler);    
        }

        public static Batch GetBatch(
            this IHandler handler, object tag = null)
        {
            var batch = handler?.Resolve<Batch>();
            if (batch == null) return null;
            return tag == null || batch.ShouldBatch(tag) ? batch : null;
        }

        public static TBatch GetBatch<TBatch>(
            this IHandler handler, object tag = null)
            where TBatch : class, IBatching, new()
        {
            var batch = handler.GetBatch(tag);
            if (batch != null)
            {
                var batcher = batch.FindHandler<TBatch>();
                if (batcher == null)
                    batch.AddHandlers(batcher = new TBatch());
                return batcher;
            }
            return null;
        }

        public static TTag GetBatch<TTag, TBatch>(
            this IHandler handler, object tag = null)
            where TBatch : class, IBatching, TTag, new()
        {
            return handler.GetBatch<TBatch>(typeof(TTag));
        }
    }
}
