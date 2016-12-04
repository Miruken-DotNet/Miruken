using System;
using SixFlags.CF.Miruken.Concurrency;

namespace SixFlags.CF.Miruken.Callback
{
    public delegate object BeforeCallback(object callback, ICallbackHandler composer);
    public delegate void AfterCallback(object callback, ICallbackHandler composer, object state);

    public static class CallbackHandlerHelpers
    {
        public static ICallbackHandler ToCallbackHandler(this object instance)
        {
            var handler = instance as ICallbackHandler;
            return handler ?? new CallbackHandler(instance);
        }

        public static bool Handle(this ICallbackHandler handler, object callback)
        {
            return handler != null && handler.Handle(callback, false, null);
        }

        public static bool Handle(this ICallbackHandler handler, object callback, bool greedy)
        {
            return handler != null && handler.Handle(callback, greedy, null);
        }

        public static ICallbackHandler Chain(
            this ICallbackHandler handler, params ICallbackHandler[] chain)
        {
            if (handler == null) return null;
            switch (chain.Length)
            {
                case 0:
                    return handler;
                case 1:
                    return new CascadeCallbackHandler(handler, chain[0]);
                default:
                {
                    var h = new object[chain.Length + 1];
                    h[0]  = handler;
                    chain.CopyTo(h, 1);
                    return new CompositeCallbackHandler(h);
                }
            }
        }

        public static CallbackHandlerFilter Filter(
            this ICallbackHandler handler, CallbackFilter filter)
        {
            return Filter(handler, filter, false);
        }

        public static CallbackHandlerFilter Filter(
            this ICallbackHandler handler, CallbackFilter filter, bool reentrant)
        {
            return handler == null ? null
                 : new CallbackHandlerFilter(handler, filter, reentrant);
        }

        public static CallbackHandlerFilter Aspect(
            this ICallbackHandler handler, BeforeCallback before)
        {
            return Aspect(handler, before, false);
        }

        public static CallbackHandlerFilter Aspect(
            this ICallbackHandler handler, BeforeCallback before, bool reentrant)
        {
            return Aspect(handler, before, null, reentrant);
        }

        public static CallbackHandlerFilter Aspect(
            this ICallbackHandler handler, AfterCallback after)
        {
            return Aspect(handler, after, false);
        }

        public static CallbackHandlerFilter Aspect(
                 this ICallbackHandler handler, AfterCallback after, bool reentrant)
        {
            return Aspect(handler, null, after, reentrant);
        }

        public static CallbackHandlerFilter Aspect(
            this ICallbackHandler handler, BeforeCallback before, AfterCallback after)
        {
            return Aspect(handler, before, after, false);
        }

        public static CallbackHandlerFilter Aspect(
            this ICallbackHandler handler, BeforeCallback before, AfterCallback after,
            bool reentrant)
        {
            return handler == null ? null
                 : handler.Filter((callback, composer, proceed) =>
            {
                object state = null;
                var cb = callback as ICallback;

                if (before != null)
                {
                    state = before(callback, composer);
                    var promise = state as Promise;
                    if (promise != null)
                    {
                        // TODO: Use Promise.End if cb.ResultType is not a Promise
                        // TODO: or you will get an InvalidCastException
                        var accept = promise.Then((accepted,s) => {
                            if (!Equals(accepted, false))
                            {
                                AspectProceed(callback, composer, proceed, after, state);
                                return Promise.Resolved(cb != null ? cb.Result : null);
                            }
                            return Promise.Rejected(new RejectedException(callback), s);
                        });                     
                        if (cb != null)
                            cb.Result = accept;
                        return true;
                    }
                    if (Equals(state, false))
                    {
                        var resultType = cb != null ? cb.ResultType : null;
                        if (resultType != null && typeof(Promise).IsAssignableFrom(cb.ResultType))
                        {
                            cb.Result = Promise.Rejected(new RejectedException(callback))
                                .Coerce(resultType);
                            return true;
                        }
                        throw new RejectedException(callback);
                    }
                }
                return AspectProceed(callback, composer, proceed, after, state);
            }, reentrant);
        }

        private static bool AspectProceed(
            object callback, ICallbackHandler composer,
            Func<bool> proceed, AfterCallback after, object state)
        {
            Promise promise = null;
            try
            {
                var handled = proceed();
                var cb = callback as ICallback;
                if (cb == null) return handled;
                promise = cb.Result as Promise;
                if (promise == null) return handled;
                if (after != null)
                    promise.Finally(() => after(callback, composer, state));
                return handled;
            }
            finally
            {
                if (after != null && promise == null)
                    after(callback, composer, state);
            }
        }
    }
}
