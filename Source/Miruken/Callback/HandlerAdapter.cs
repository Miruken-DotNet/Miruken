namespace Miruken.Callback
{
    using System;

    public class HandlerAdapter : Handler
    {
        public HandlerAdapter(object handler)
        {
            Handler = handler 
                   ?? throw new ArgumentNullException(nameof(handler));
        }

        public object Handler { get; }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            return Dispatch(Handler, callback, ref greedy, composer);
        }
    }
}
