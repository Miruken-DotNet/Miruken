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

    public class Batch : CompositeHandler, IBatchingComplete
    {
        private readonly HashSet<object> _tags;

        public Batch(params object[] tags)
        {
            _tags = new HashSet<object>(tags);
        }

        public bool ShouldBatch(object tag)
        {
            return _tags.Count == 0 || _tags.Contains(tag);
        }

        Promise<object[]> IBatchingComplete.Complete(IHandler composer)
        {
            var results = Handlers.Select(handler =>
                handler.Proxy<IBatching>().Complete(composer));
            return Promise.All(results);
        }
    }

    public class NoBatch : Trampoline, IBatchCallback
    {
        public NoBatch(object callback)
            : base(callback)
        {
        }

        bool IBatchCallback.AllowBatching => false;
    }
}
