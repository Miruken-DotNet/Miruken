namespace Miruken.Callback
{
    using System;
    using Policy;

    public class Trampoline : IDispatchCallback
    {
        public Trampoline(object callback)
        {
            Callback = callback 
                    ?? throw new ArgumentNullException(nameof(callback));
        }

        protected Trampoline()
        {
        }
           
        public object Callback { get; }

        CallbackPolicy IDispatchCallback.Policy =>
            (Callback as IDispatchCallback)?.Policy;

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            return Callback != null &&
                Handler.Dispatch(handler, Callback, ref greedy, composer);
        }
    }
}
