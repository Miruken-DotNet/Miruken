namespace Miruken.Callback
{
    using System;
    using System.Diagnostics;
    using Policy;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Resolving : Inquiry, IInferCallback, IFilterCallback
    {
        private readonly object _callback;
        private bool _handled;

        public Resolving(object key, object callback)
            : base(key, callback as Inquiry, true)
        {
            _callback = callback
                ?? throw new ArgumentNullException(nameof(callback));
        }

        bool IFilterCallback.CanFilter => false;

        object IInferCallback.InferCallback()
        {
            return this;
        }

        public override bool CanDispatch(
            object target, MemberDispatch dispatcher)
        {
            return (_callback as IDispatchCallbackGuard)
                   ?.CanDispatch(target, dispatcher) != false;
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
