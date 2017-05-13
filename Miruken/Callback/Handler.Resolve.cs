namespace Miruken.Callback
{
    using System;
    using System.Collections;
    using System.Linq;
    using Concurrency;

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
            var inquiry = key as Inquiry ?? new Inquiry(key);
            if (handler.Handle(inquiry))
            {
                var result = inquiry.Result;
                return inquiry.IsAsync 
                     ? ((Promise)result).Wait()
                     : result;
            }
            return null;
        }

        public static Promise ResolveAsync(this IHandler handler, object key)
        {
            if (handler == null) return null;
            var inquiry = key as Inquiry ?? new Inquiry(key);
            inquiry.WantsAsync = true;
            return handler.Handle(inquiry)
                 ? (Promise)inquiry.Result
                 : Promise.Empty;
        }

        public static T Resolve<T>(this IHandler handler)
        {
            return handler == null ? default(T)
                 : (T)Resolve(handler, typeof(T));
        }

        public static Promise<T> ResolveAsync<T>(this IHandler handler)
        {
            return handler == null ? Promise<T>.Empty
                 : (Promise<T>)ResolveAsync(handler, typeof(T))
                 .Coerce(typeof(Promise<T>));
        }

        public static object[] ResolveAll(this IHandler handler, object key)
        {
            if (handler == null)
                return Array.Empty<object>();
            var inquiry = key as Inquiry ?? new Inquiry(key, true);
            if (handler.Handle(inquiry, true))
            {
                var result = inquiry.Result;
                return inquiry.IsAsync
                     ? ((Promise<object[]>)result).Wait()
                     : (object[])result;
            }
            return Array.Empty<object>();
        }

        public static Promise<object[]> ResolveAllAsync(this IHandler handler, object key)
        {
            if (handler == null)
                return Promise.Resolved(Array.Empty<object>());
            var inquiry = key as Inquiry ?? new Inquiry(key, true);
            inquiry.WantsAsync = true;
            if (handler.Handle(inquiry, true))
            {
                var result = inquiry.Result;
                return inquiry.IsAsync
                     ? (Promise<object[]>)result
                     : Promise.Resolved((object[])result);
            }
            return Promise.Resolved(Array.Empty<object>());
        }

        public static T[] ResolveAll<T>(this IHandler handler)
        {
            if (handler == null)
                return Array.Empty<T>();
            var results = ResolveAll(handler, typeof(T));
            return results?.Cast<T>().ToArray() ?? Array.Empty<T>();
        }

        public static Promise<T[]> ResolveAllAsync<T>(this IHandler handler)
        {
            return handler == null ? Promise.Resolved(Array.Empty<T>())
                 : ResolveAllAsync(handler, typeof(T))
                      .Then((r, s) => r?.Cast<T>().ToArray() 
                                   ?? Array.Empty<T>());
        }
    }
}
