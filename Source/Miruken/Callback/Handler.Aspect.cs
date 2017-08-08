namespace Miruken.Callback
{
    using System;
    using Concurrency;
    using Infrastructure;

    public delegate object BeforeCallback(object callback, IHandler composer);
    public delegate void AfterCallback(object callback, IHandler composer, object state);

    public static class HandlerAspectExtensions
    {

        public static HandlerFilter Aspect(
            this IHandler handler, BeforeCallback before)
        {
            return Aspect(handler, before, false);
        }

        public static HandlerFilter Aspect(
            this IHandler handler, BeforeCallback before, bool reentrant)
        {
            return Aspect(handler, before, null, reentrant);
        }

        public static HandlerFilter Aspect(
            this IHandler handler, AfterCallback after)
        {
            return Aspect(handler, after, false);
        }

        public static HandlerFilter Aspect(
                 this IHandler handler, AfterCallback after, bool reentrant)
        {
            return Aspect(handler, null, after, reentrant);
        }

        public static HandlerFilter Aspect(
            this IHandler handler, BeforeCallback before, AfterCallback after)
        {
            return Aspect(handler, before, after, false);
        }

        public static HandlerFilter Aspect(
            this IHandler handler, BeforeCallback before, AfterCallback after,
            bool reentrant)
        {
            return handler?.Filter((callback, composer, proceed) =>
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
                        var accept = promise.Then((accepted, s) => {
                            if (!Equals(accepted, false))
                            {
                                AspectProceed(callback, composer, proceed, after, state);
                                return Promise.Resolved(cb?.Result);
                            }
                            return Promise.Rejected(new RejectedException(callback), s);
                        });
                        if (cb != null)
                            cb.Result = accept;
                        return true;
                    }
                    if (Equals(state, false))
                    {
                        var resultType = cb?.ResultType;
                        if (resultType.Is<Promise>())
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
            object callback, IHandler composer,
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
