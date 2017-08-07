namespace Miruken.Callback
{
    using System;

    public class Resolve : Inquiry, IResolveCallback
    {
        private readonly object _callback;
        private bool _handled;

        public Resolve(object key, object callback)
            : base(key, true)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            _callback = callback;
        }

        protected override bool IsSatisfied(
            object resolution, bool greedy, IHandler composer)
        {
            if (_handled && !greedy) return true;
            return _handled = 
                Handler.Dispatch(resolution, _callback, ref greedy, composer)
                || _handled;
        }

        object IResolveCallback.GetResolveCallback()
        {
            return this;
        }
    }
}
