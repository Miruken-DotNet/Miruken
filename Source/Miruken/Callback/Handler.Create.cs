namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using Concurrency;

    public static class HandlerCreateExtensions
    {
        public static object Create(this IHandler handler, Type type)
        {
            if (handler == null) return null;
            var creation = new Creation(type);
            return handler.Handle(creation) ? creation.Result
                 : throw new NotSupportedException($"Unable to create instance of {type.FullName}.  Did you forget to add a [Creates] attribute to the constructors?");
        }

        public static Promise CreateAsync(this IHandler handler, Type type)
        {
            if (handler == null) return null;
            var creation = new Creation(type) { WantsAsync = true };
            try
            {
                return handler.Handle(creation)
                     ? (Promise)creation.Result
                     : Promise.Rejected(new NotSupportedException(
                         $"Unable to create instance of {type.FullName}.  Did you forget to add a [Creates] attribute to the constructors?"));
            }
            catch (Exception ex)
            {
                return Promise.Rejected(ex);
            }
        }

        public static T Create<T>(this IHandler handler)
        {
            return handler == null ? default : (T)Create(handler, typeof(T));
        }

        public static Promise<T> CreateAsync<T>(this IHandler handler)
        {
            return handler == null ? Promise<T>.Empty
                 : (Promise<T>)CreateAsync(handler, typeof(T))
                    .Coerce(typeof(Promise<T>));
        }

        public static object[] CreateAll(this IHandler handler, Type type)
        {
            if (handler != null)
            {
                var creation = new Creation(type, true);
                if (handler.Handle(creation, true))
                {
                    var result = creation.Result;
                    return CoerceArray((object[])result, type);
                }
            }
            return CoerceArray(Array.Empty<object>(), type);
        }

        public static Promise<object[]> CreateAllAsync(this IHandler handler, Type type)
        {
            if (handler != null)
            {
                var creation = new Creation(type, true)
                {
                    WantsAsync = true
                };
                if (handler.Handle(creation, true))
                {
                    var result = creation.Result;
                    return ((Promise<object[]>)result)
                        .Then((arr, s) => CoerceArray(arr, type));
                }
            }
            var empty = CoerceArray(Array.Empty<object>(), type);
            return Promise.Resolved(empty);
        }

        public static T[] CreateAll<T>(this IHandler handler)
        {
            if (handler == null)
                return Array.Empty<T>();
            var results = CreateAll(handler, typeof(T));
            return results?.Cast<T>().ToArray() ?? Array.Empty<T>();
        }

        public static Promise<T[]> CreateAllAsync<T>(this IHandler handler)
        {
            return handler == null ? Promise.Resolved(Array.Empty<T>())
                : CreateAllAsync(handler, typeof(T))
                    .Then((r, s) => r?.Cast<T>().ToArray() ?? Array.Empty<T>());
        }

        private static object[] CoerceArray(object[] array, Type type)
        {
            var typed = Array.CreateInstance(type, array.Length);
            array.CopyTo(typed, 0);
            return (object[])typed;
        }
    }
}
