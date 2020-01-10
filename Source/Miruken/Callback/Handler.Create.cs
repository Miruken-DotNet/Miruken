namespace Miruken.Callback
{
    using System;
    using Concurrency;

    public static class HandlerCreateExtensions
    {
        public static object Create(this IHandler handler, Type type)
        {
            if (handler == null) return null;
            var creation = new Creation(type);
            return handler.Handle(creation) ? creation.Result :
                throw new NotSupportedException($"Unable to create instance of {type.FullName}.  Did you forget to add a [Creates] attribute to the constructors?");
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

        public static T Create<T>(this IHandler handler) where T : class
        {
            return handler == null ? default : (T)Create(handler, typeof(T));
        }

        public static Promise<T> CreateAsync<T>(this IHandler handler) where T : class
        {
            return handler == null ? Promise<T>.Empty
                 : (Promise<T>)CreateAsync(handler, typeof(T))
                    .Coerce(typeof(Promise<T>));
        }
    }
}
