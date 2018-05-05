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

        public static IHandler Resolve<T>(
            this IHandler handler, Func<T> provider, out T result)
        {
            var inquiry = new Inquiry(typeof(T));
            if (handler.Handle(inquiry))
            {
                result = (T)inquiry.Result;
                return handler;
            }
            result = provider();
            return handler.Provide(result);
        }

        public static T Rssolve<T>(
            this IHandler handler, Func<T> provider, out IHandler composer)
        {
            composer = Resolve(handler, provider, out var result);
            return result;
        }

        public static IHandler Provide<T>(this IHandler handler, T result)
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
