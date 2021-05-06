namespace Miruken.Api
{
    using System;
    using System.Threading.Tasks;
    using Callback;

    public static class HandlerStashExtensions
    {
        public static T StashGet<T>(this IHandler handler) where T : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var get = new StashAction.Get(typeof(T));
            if (!handler.Handle(get))
                throw new NotSupportedException($"Stash get {typeof(T)} not handled.");
            return get.Value as T;
        }

        public static void StashPut<T>(this IHandler handler, T data) where T : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var put = new StashAction.Put(typeof(T), data);
            if (!handler.Handle(put))
                throw new NotSupportedException($"Stash put {typeof(T)} not handled.");
        }

        public static void StashDrop<T>(this IHandler handler) where T : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var drop = new StashAction.Drop(typeof(T));
            if (!handler.Handle(drop))
                throw new NotSupportedException($"Stash drop {typeof(T)} not handled.");
        }

        public static T StashTryGet<T>(this IHandler handler) where T : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var get = new StashAction.Get(typeof(T));
            return handler.Handle(get) ? get.Value as T : null;
        }

        public static T StashGetOrPut<T>(this IHandler handler, T put)
            where T : class
        {
            var data = handler.StashTryGet<T>();
            if (data == null)
            {
                data = put;
                handler.StashPut(data);
            }
            return data;
        }

        public static T StashGetOrPut<T>(this IHandler handler, Func<T> put)
            where T : class
        {
            if (put == null)
                throw new ArgumentNullException(nameof(put));
            var data = handler.StashTryGet<T>();
            if (data == null)
            {
                data = put();
                handler.StashPut(data);
            }
            return data;
        }

        public static async Task<T> StashGetOrPut<T>(
            this IHandler handler, Func<Task<T>> put)
            where T : class
        {
            if (put == null)
                throw new ArgumentNullException(nameof(put));
            var data = handler.StashTryGet<T>();
            if (data == null)
            {
                data = await put().ConfigureAwait(false);;
                handler.StashPut(data);
            }
            return data;
        }
    }
}
