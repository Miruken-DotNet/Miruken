using System.Collections;
using System.Linq;

namespace Miruken.Callback
{
    using System;

    public partial class Handler
    {
        object IServiceProvider.GetService(Type service)
        {
            return this.Resolve(service);
        }
    }

    public static class HandlerResolveExtensions
    {
        public static object Resolve(this IHandler handler, object key)
        {
            if (handler == null) return null;
            var resolution = key as Resolution ?? new Resolution(key);
            return handler.Handle(resolution)
                 ? resolution.Result
                 : null;
        }

        public static T Resolve<T>(this IHandler handler)
        {
            if (handler == null) return default(T);
            return (T) Resolve(handler, typeof(T));
        }

        public static object[] ResolveAll(this IHandler handler, object key)
        {
            if (handler == null) return new object[0];
            var resolution = key as Resolution ?? new Resolution(key, true);
            return handler.Handle(resolution, true)
                 ? EnsureArray(resolution.Result)
                 : new object[0];
        }

        public static T[] ResolveAll<T>(this IHandler handler)
        {
            if (handler == null) return new T[0];
            var results = ResolveAll(handler, typeof (T));
            return results?.Cast<T>().ToArray() ?? new T[0];
        }

        private static object[] EnsureArray(object array)
        {
            return array as object[] ?? ((IEnumerable)array).Cast<object>().ToArray();
        }
    }
}
