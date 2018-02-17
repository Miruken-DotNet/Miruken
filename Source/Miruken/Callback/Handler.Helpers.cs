namespace Miruken.Callback
{
    using System;

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
            return new Provider(result) + handler;
        }

        public static FilteredHandler Filter(
            this IHandler handler, HandlerFilterDelegate filter,
            bool reentrant = false)
        {
            return handler == null ? null
                 : new FilteredHandler(handler, filter, reentrant);
        }
    }
}
