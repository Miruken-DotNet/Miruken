namespace Miruken.Callback
{
    using System;

    public class Resolving : Inquiry, IResolveCallback
    {
        private bool _handled;

        public Resolving(object key, object callback)
            : base(key, true)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            Callback = callback;
        }

        public object Callback { get; }

        object IResolveCallback.GetResolveCallback()
        {
            return this;
        }

        protected override bool IsSatisfied(
            object resolution, bool greedy, IHandler composer)
        {
            if (_handled && !greedy) return true;
            return _handled = 
                Handler.Dispatch(resolution, Callback, ref greedy, composer)
                || _handled;
        }

        public static object GetDefaultResolvingCallback(object callback)
        {
            var dispatch = callback as IDispatchCallback;
            var policy   = dispatch?.Policy ?? HandlesAttribute.Policy;
            var handlers = policy.GetHandlers(callback);
            var bundle   = new Bundle(false);
            foreach (var handler in handlers)
                bundle.Add(h => h.Handle(new Resolving(handler, callback)));
            return bundle;
        }
    }
}
