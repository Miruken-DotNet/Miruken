namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using Infrastructure;

    public static class HandlerHelpers
    {
        public static bool Handle(this IHandler handler, object callback,
          bool greedy = false, IHandler composer = null)
        {
            return handler.Handle(callback, ref greedy, composer);
        }

        public static IHandler Chain(
            this IHandler handler, params object[] chain)
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
                    h[0] = handler;
                    chain.CopyTo(h, 1);
                    return new CompositeHandler(h);
                }
            }
        }

        public static IHandler Resolve<R>(
            this IHandler handler, Func<R> provider, out R result)
        {
            var inquiry = new Inquiry(typeof(R));
            if (handler.Handle(inquiry))
            {
                result = (R)inquiry.Result;
                return handler;
            }
            result = provider();
            return handler.Provide(result);
        }

        public static R Rssolve<R>(
            this IHandler handler, Func<R> provider, out IHandler composer)
        {
            R result;
            composer = Resolve(handler, provider, out result);
            return result;
        }

        public static IHandler Provide<R>(this IHandler handler, R result)
        {
            return Provide(handler, (inquiry, greedy, composer) =>
            {
                var type = inquiry.Key as Type;
                if (type.Is<R>())
                {
                    inquiry.Resolve(result, greedy, composer);
                    return true;
                }
                return false;
            });
        }

        public static IHandler ProvideMany<R>(this IHandler handler, IEnumerable<R> result)
        {
            return Provide(handler, (inquiry, greedy, composer) =>
            {
                var resolved = false;
                var type     = inquiry.Key as Type;
                if (type.Is<R>())
                {
                    foreach (var r in result)
                    {
                        resolved = inquiry.Resolve(r, greedy, composer) || resolved;
                        if (resolved && !inquiry.Many) return true;
                    }
                }
                return resolved;
            });
        }

        public static IHandler Provide(this IHandler handler, ProvidesDelegate provider)
        {
            return new Provider(provider) + handler;
        }

        public static HandlerFilter Filter(
            this IHandler handler, HandlerFilterDelegate filter)
        {
            return Filter(handler, filter, false);
        }

        public static HandlerFilter Filter(
            this IHandler handler, HandlerFilterDelegate filter, bool reentrant)
        {
            return handler == null ? null
                 : new HandlerFilter(handler, filter, reentrant);
        }
    }
}
