namespace Miruken.Callback
{
    using System.Collections.Generic;
    using System.Linq;
    using Concurrency;

    public interface IBatching
    {
        object Complete(IHandler composer);
    }

    public interface IBatchingComplete
    {
        Promise<object[]> Complete(IHandler composer);
    }

    public sealed class Batch : CompositeHandler, IBatchingComplete
    {
        private readonly HashSet<object> _tags;

        public Batch(params object[] tags)
        {
            if (tags != null)
                _tags = new HashSet<object>(tags);
        }

        public bool ShouldBatch(object tag)
        {
            return _tags == null || _tags.Count == 0
                || _tags.Contains(tag);
        }

        Promise<object[]> IBatchingComplete.Complete(IHandler composer)
        {
            var results = Handlers.Select(handler =>
                handler.Proxy<IBatching>().Complete(composer));
            return Promise.All(results);
        }
    }

    public sealed class NoBatch : Trampoline, IBatchCallback, IInferCallback
    {
        public NoBatch(object callback)
            : base(callback)
        {
        }

        bool IBatchCallback.CanBatch => false;

        object IInferCallback.InferCallback()
        {
            return this;
        }
    }
}
