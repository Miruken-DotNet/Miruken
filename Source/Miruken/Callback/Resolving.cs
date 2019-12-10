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
        private readonly object _callback;
        private bool _handled;

        public Resolving(object key, object callback)
            : base(key, callback as Inquiry, true)
        {
            _callback = callback
                ?? throw new ArgumentNullException(nameof(callback));
        }

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
            var innerCheck = (_callback as IDispatchCallbackGuard)
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
            if (_handled && !greedy) return true;
            return _handled = Handler.Dispatch(
                resolution, _callback, ref greedy, composer)
                || _handled;
        }

        private string DebuggerDisplay => $"Resolving | {Key} => {_callback}";
    }
}
