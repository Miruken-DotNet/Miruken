namespace Miruken.Callback
{
    using System;
    using Concurrency;
    using Infrastructure;

    public delegate object BeforeCallback(object callback, IHandler composer);
    public delegate void AfterCallback(object callback, IHandler composer, object state);

    public static class HandlerAspectExtensions
    {
        public static IHandler Aspect(
            this IHandler handler, BeforeCallback before, bool reentrant = false)
        {
            return Aspect(handler, before, null, reentrant);
        }

        public static IHandler Aspect(
            this IHandler handler, AfterCallback after, bool reentrant = false)
        {
            return Aspect(handler, null, after, reentrant);
        }

        public static IHandler Aspect(
            this IHandler handler, BeforeCallback before, AfterCallback after,
            bool reentrant = false)
        {
            if (before == null && after == null) return handler;
            return handler?.Filter((callback, composer, proceed) =>
            {
                var cb = callback as ICallback;

                if (before == null)
                    return AspectProceed(callback, composer, proceed, after, null);
                var state = before(callback, composer);
                switch (state)
                {
                    case Promise promise:
                        var accept = promise.Then((accepted, s) => {
                            if (Equals(accepted, false))
                                return Promise.Rejected(new RejectedException(callback), s);
                            AspectProceed(callback, composer, proceed, after, state);
                            return Promise.Resolved(cb?.Result);
                        });
                        if (cb != null) cb.Result = accept;
                        return true;
                    case false:
                        var resultType = cb?.ResultType;
                        if (!resultType.Is<Promise>())
                            throw new RejectedException(callback);
                        if (cb != null)
                            cb.Result = Promise.Rejected(new RejectedException(callback))
                                .Coerce(resultType);
                        return true;
                }
                return AspectProceed(callback, composer, proceed, after, state);
            }, reentrant);
        }

        private static bool AspectProceed(
            object callback, IHandler composer,
            Func<bool> proceed, AfterCallback after, object state)
        {
            if (after == null) return proceed();
            Promise promise = null;
            try
            {
                var handled = proceed();
                var cb = callback as ICallback;
                if (cb == null) return handled;
                promise = cb.Result as Promise;
                promise?.Finally(() => after(callback, composer, state));
                return handled;
            }
            finally
            {
                if (promise == null)
                    after(callback, composer, state);
            }
        }
    }
}
