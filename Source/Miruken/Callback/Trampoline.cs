namespace Miruken.Callback
{
    using System;
    using Policy;
    using Policy.Bindings;

    public class Trampoline : ICallback,
        IDispatchCallback, IDispatchCallbackGuard
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

        public Type ResultType
        {
            get
            {
                var cb = Callback as ICallback;
                return cb?.ResultType;
            }
        }

        public object Result
        {
            get
            {
                var cb = Callback as ICallback;
                return cb?.Result;
            }

            set
            {
                if (Callback is ICallback cb)
                    cb.Result = value;
            }
        }

        CallbackPolicy IDispatchCallback.Policy =>
            (Callback as IDispatchCallback)?.Policy;

        public bool CanDispatch(object target,
            PolicyMemberBinding binding, MemberDispatch dispatcher)
        {
            return (Callback as IDispatchCallbackGuard)
                   ?.CanDispatch(target, binding, dispatcher) != false;
        }

        public virtual bool Dispatch(object handler,
            ref bool greedy, IHandler composer)
        {
            return Callback != null
                  ? Handler.Dispatch(handler, Callback, ref greedy, composer)
                  : new Command(this).Dispatch(handler, ref greedy, composer);
        }
    }
}
