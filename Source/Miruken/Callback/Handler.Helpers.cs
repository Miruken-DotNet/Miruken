namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using Policy;

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

        public static IHandler Provide<T>(this IHandler handler, T result)
        {
            return new Provider(result) + handler;
        }

        public static IHandler With<T>(this IHandler handler, T result)
        {
            return new Provider(result) + handler;
        }

        public static object[] ResolveArgs(this IHandler handler, params Argument[] args)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (args.Length == 0) return Array.Empty<object>();
            if (args.Any(arg => arg == null))
                throw new ArgumentException("One or more null args provided");
            var resolver = ResolvingAttribute.Default;
            var resolved = new object[args.Length];
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = resolver.ResolveArgument(null, args[i], handler);
                if (arg == null) return null;
                resolved[i] = arg;
            }
            return resolved;
        }

        public static TargetActionBuilder<IHandler> Target(this IHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            return new TargetActionBuilder<IHandler>(action =>
            {
                if (!action(handler, args => ResolveArgs(handler, args)))
                    throw new InvalidOperationException(
                        "One more or arguments could not be resolved");
            });
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
