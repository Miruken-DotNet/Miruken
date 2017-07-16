namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using Concurrency;
    using Infrastructure;

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
            var type = key as Type;
            return type != null 
                 ? RuntimeHelper.GetDefault(type)
                 : null;
        }

        public static Promise ResolveAsync(this IHandler handler, object key)
        {
            if (handler == null) return null;
            var inquiry = key as Inquiry ?? new Inquiry(key);
            inquiry.WantsAsync = true;
            if (handler.Handle(inquiry))
                return (Promise)inquiry.Result;
            var type = key as Type;
            return type != null 
                 ? Promise.Resolved(RuntimeHelper.GetDefault(type))
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
            if (handler != null)
            {
                var inquiry = key as Inquiry ?? new Inquiry(key, true);
                if (handler.Handle(inquiry, true))
                {
                    var result = inquiry.Result;
                    return CoerceArray(inquiry.IsAsync
                         ? ((Promise<object[]>)result).Wait()
                         : (object[]) result, key);
                }
            }
            return CoerceArray(Array.Empty<object>(), key);
        }

        public static Promise<object[]> ResolveAllAsync(this IHandler handler, object key)
        {
            if (handler != null)
            {
                var inquiry = key as Inquiry ?? new Inquiry(key, true);
                inquiry.WantsAsync = true;
                if (handler.Handle(inquiry, true))
                {
                    var result = inquiry.Result;
                    return inquiry.IsAsync
                         ? ((Promise<object[]>)result)
                                .Then((arr,s) => CoerceArray(arr, key))
                         : Promise.Resolved(CoerceArray((object[])result, key));
                }
            }
            var empty = CoerceArray(Array.Empty<object>(), key);
            return Promise.Resolved(empty);
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

        private static object[] CoerceArray(object[] array, object key)
        {
            var type = key as Type;
            if (type == null) return array;
            var typed = Array.CreateInstance(type, array.Length);
            array.CopyTo(typed, 0);
            return (object[])typed;
        }
    }
}
