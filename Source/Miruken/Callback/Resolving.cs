namespace Miruken.Callback
{
    using System;
    using System.Diagnostics;
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
            PolicyMemberBinding binding, MemberDispatch dispatcher)
        {
            return base.CanDispatch(target, binding, dispatcher) &&
                (_callback as IDispatchCallbackGuard)
                   ?.CanDispatch(target, binding, dispatcher) != false;
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
