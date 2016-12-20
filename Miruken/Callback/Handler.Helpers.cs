using System;
using Miruken.Concurrency;

namespace Miruken.Callback
{
    public delegate object BeforeCallback(object callback, IHandler composer);
    public delegate void AfterCallback(object callback, IHandler composer, object state);

    public static class HandlerHelpers
    {
        public static IHandler ToHandler(this object instance)
        {
            var handler = instance as IHandler;
            return handler ?? new Handler(instance);
        }

        public static IHandler Chain(
            this IHandler handler, params IHandler[] chain)
        {
            if (handler == null) return null;
            switch (chain.Length)
            {
                case 0:
                    return handler;
                case 1:
                    return new CascadeHandler(handler, chain[0]);
                default:
                {
                    var h = new object[chain.Length + 1];
                    h[0]  = handler;
                    chain.CopyTo(h, 1);
                    return new CompositeHandler(h);
                }
            }
        }

        public static IHandler Provide<R>(this IHandler handler, R result)
        {
            return Provide(handler, (resolution, composer) =>
            {
                var type = resolution.Key as Type;
                if (type?.IsAssignableFrom(typeof(R)) == true)
                {
                    resolution.Resolve(result, composer);
                    return true;
                }
                return false;
            });
        }

        public static IHandler Provide(this IHandler handler, ProviderDelegate provider)
        {
            return new Provider(provider) + handler;
        }

        public static HandlerFilter Filter(
            this IHandler handler, CallbackFilter filter)
        {
            return Filter(handler, filter, false);
        }

        public static HandlerFilter Filter(
            this IHandler handler, CallbackFilter filter, bool reentrant)
        {
            return handler == null ? null
                 : new HandlerFilter(handler, filter, reentrant);
        }

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
                        var accept = promise.Then((accepted,s) => {
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
