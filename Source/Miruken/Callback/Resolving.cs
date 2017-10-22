namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using Policy;

    public class Resolving : Inquiry, IResolveCallback
    {
        private readonly object _callback;
        private bool _handled;

        public Resolving(object key, object callback)
            : base(key, true)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            _callback = callback;
        }

        object IResolveCallback.GetResolveCallback()
        {
            return this;
        }

        protected override bool IsSatisfied(
            object resolution, bool greedy, IHandler composer)
        {
            if (_handled && !greedy) return true;
            return _handled = Handler.Dispatch(
                resolution, _callback, ref greedy, composer)
                || _handled;
        }

        public static object GetDefaultResolvingCallback(object callback)
        {
            var handlers = CallbackPolicy.GetCallbackHandlers(callback).ToArray();
            if (handlers.Length == 0) return callback;
            var bundle = new Bundle(false)
                .Add(h => h.Handle(new NoResolving(callback)), handled => handled);
            foreach (var handler in handlers)
                bundle.Add(h => h.Handle(new Resolving(handler, callback)));
            return bundle;
        }
    }

    public class NoResolving : Trampoline, IResolveCallback
    {
        public NoResolving(object callback)
            : base(callback)
        {
        }

        object IResolveCallback.GetResolveCallback()
        {
            return Callback;
        }
    }
}
