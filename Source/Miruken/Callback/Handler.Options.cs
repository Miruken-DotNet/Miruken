namespace Miruken.Callback
{
    public static class OptionExtensions
    {
        public static IHandler WithOptions<T>(this IHandler handler, T options)
            where T : Options<T>
        {
            return handler == null || options == null
                ? null
                : options.Decorate(handler);
        }

        public static T GetOptions<T>(this IHandler handler, T options)
            where T : Options<T>
        {
            return handler == null || options == null ? null
                : handler.Handle(options, true) ? options : null;
        }

        public static T GetOptions<T>(this IHandler handler)
            where T : Options<T>, new()
        {
            if (handler == null) return null;
            var options = new T();
            return handler.Handle(options, true) ? options : null;
        }
    }
}