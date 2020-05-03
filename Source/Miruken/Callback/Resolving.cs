namespace Miruken.Callback
{
    using System;
    using System.Diagnostics;
    using Infrastructure;
    using Policy;
    using Policy.Bindings;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Resolving : Inquiry, IInferCallback
    {
        public Resolving(object key, object callback)
            : base(key, callback as Inquiry, true)
        {
            Callback = callback
                ?? throw new ArgumentNullException(nameof(callback));
        }

        public object Callback  { get; }
        public bool   Succeeded { get; private set; }

        object IInferCallback.InferCallback()
        {
            return this;
        }

        public override bool CanDispatch(object target,
            PolicyMemberBinding binding, MemberDispatch dispatcher,
            out IDisposable reset)
        {
            reset = null;
            if (!base.CanDispatch(target, binding, dispatcher, out var outer))
                return false;

            IDisposable inner = null;
            var innerCheck = (Callback as IDispatchCallbackGuard)
                ?.CanDispatch(target, binding, dispatcher, out inner);
            switch (innerCheck)
            {
                case null:
                    reset = outer;
                    return true;
                case false:
                    outer.Dispose();
                    return false;
                default:
                    reset = new DisposableAction(() =>
                    {
                        inner?.Dispose();
                        outer?.Dispose();
                    });
                    return true;
            }
        }

        protected override bool IsSatisfied(
            object resolution, bool greedy, IHandler composer)
        {
            if (Succeeded && !greedy) return true;
            var handled = Handler.Dispatch(resolution, Callback, ref greedy, composer);
            if (handled) Succeeded = true;
            return handled;
        }

        private string DebuggerDisplay => $"Resolving | {Key} => {Callback}";
    }
}
