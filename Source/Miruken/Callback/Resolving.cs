﻿namespace Miruken.Callback
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Policy;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Resolving : Inquiry,
        IResolveCallback, IFilterCallback, IDispatchCallbackGuard
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

        object IResolveCallback.GetResolveCallback()
        {
            return this;
        }

        bool IDispatchCallbackGuard.CanDispatch(
            object handler, PolicyMemberBinding binding)
        {
            return (_callback as IDispatchCallbackGuard)
                   ?.CanDispatch(handler, binding) != false;
        }

        protected override bool IsSatisfied(
            object resolution, bool greedy, IHandler composer)
        {
            if (_handled && !greedy) return true;
            return _handled = Handler.Dispatch(
                resolution, _callback, ref greedy, composer)
                || _handled;
        }

        public static object GetDefaultResolvingCallback(object callback)
        {
            var handlers = CallbackPolicy.GetCallbackHandlers(callback).ToArray();
            if (handlers.Length == 0) return callback;
            var bundle = new Bundle(false)
                .Add(h => h.Handle(new NoResolving(callback)), 
                    (ref bool handled) => handled);
            foreach (var handler in handlers)
                bundle.Add(h => h.Handle(new Resolving(handler, callback)));
            return bundle;
        }

        private string DebuggerDisplay => $"Resolving | {Key} => {_callback}";
    }

    public sealed class NoResolving : Trampoline, IResolveCallback
    {
        public NoResolving(object callback)
            : base(callback)
        {
        }

        object IResolveCallback.GetResolveCallback()
        {
            return Callback;
        }
    }
}
