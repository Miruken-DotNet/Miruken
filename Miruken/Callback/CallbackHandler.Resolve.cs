using System.Collections;
using System.Linq;

namespace Miruken.Callback
{
    public static class CallbackHandlerResolveExtensions
    {
        public static object Resolve(this ICallbackHandler handler, object key)
        {
            if (handler == null) return null;
            var resolution = key as Resolution ?? new Resolution(key);
            return handler.Handle(resolution, false, HandleMethod.Composer)
                 ? resolution.Result
                 : null;
        }

        public static T Resolve<T>(this ICallbackHandler handler)
        {
            if (handler == null) return default(T);
            return (T) Resolve(handler, typeof(T));
        }

        public static object[] ResolveAll(this ICallbackHandler handler, object key)
        {
            if (handler == null) return new object[0];
            var resolution = key as Resolution ?? new Resolution(key, true);
            return handler.Handle(resolution, true, HandleMethod.Composer)
                 ? EnsureArray(resolution.Result)
                 : new object[0];
        }

        public static T[] ResolveAll<T>(this ICallbackHandler handler)
        {
            if (handler == null) return new T[0];
            var results = ResolveAll(handler, typeof (T));
            return results != null 
                 ? results.Cast<T>().ToArray()
                 : new T[0];
        }

        private static object[] EnsureArray(object array)
        {
            return array as object[] ?? (array as IEnumerable).Cast<object>().ToArray();
        }
    }
}
