namespace Miruken.Callback
{
    using System;

    public class Resolve : Inquiry
    {
        private readonly object _callback;

        public Resolve(object key, bool many, object callback)
            : base(key, many)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            _callback = callback;
        }

        protected override bool IsSatisfied(object resolution, IHandler composer)
        {
            var greedy = Many;
            return Handler.Dispatch(resolution, _callback, ref greedy, composer);
        }
    }
}
