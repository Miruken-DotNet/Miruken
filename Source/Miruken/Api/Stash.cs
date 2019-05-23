namespace Miruken.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;

    public interface IStash
    {
        T    Get<T>() where T : class;
        void Put<T>(T data) where T : class;
        bool Drop<T>() where T : class;
    }

    public class Stash : Handler, IStash
    {
        private readonly bool _root;
        private readonly Dictionary<Type, object> _data;

        public Stash(bool root = false)
        {
            _root = root;
            _data = new Dictionary<Type, object>();
        }

        [Provides]
        public T Provides<T>() where T : class
        {
            return _data.TryGetValue(typeof(T), out var data)
                 ? (T)data : null;
        }

        [Provides]
        public StashOf<T> Wraps<T>(IHandler composer) where T : class
        {
            return new StashOf<T>(composer);
        }

        public T Get<T>() where T : class
        {
            return _data.TryGetValue(typeof(T), out var data)
                 ? (T)data : (_root ? null : Unhandled<T>());
        }

        public void Put<T>(T data) where T : class
        {
            _data[typeof(T)] = data;
        }

        public bool Drop<T>() where T : class
        {
            return _data.Remove(typeof(T));
        }
    }

    public class StashOf<T>
        where T : class
    {
        private readonly IHandler _handler;
        private readonly IStash _stash;

        public StashOf(IHandler handler)
        {
            _handler = handler
                    ?? throw new ArgumentNullException(nameof(handler));
            _stash   = handler.Proxy<IStash>();
        }

        public T Value
        {
            get => _stash.TryGet<T>();
            set => _stash.Put(value);
        }

        public T GetOrPut(T value)
        {
            return _stash.GetOrPut(value);
        }

        public T GetOrPut(Func<IHandler, T> put)
        {
            if (put == null)
                throw new ArgumentNullException(nameof(put));
            return _stash.GetOrPut(() => put(_handler));
        }

        public Task<T> GetOrPut(Func<IHandler, Task<T>> put)
        {
            if (put == null)
                throw new ArgumentNullException(nameof(put));
            return _stash.GetOrPut(() => put(_handler));
        }

        public void Drop()
        {
            _stash.Drop<T>();
        }

        public static implicit operator T(StashOf<T> stashOf)
        {
            return stashOf.Value;
        }
    }

    public static class StashExtensions
    {
        public static T TryGet<T>(this IStash stash)
            where T : class
        {
            try
            {
                return stash.Get<T>();
            }
            catch
            {
                return null;
            }
        }

        public static T GetOrPut<T>(this IStash stash, T put)
            where T : class
        {
            var data = stash.TryGet<T>();
            if (data == null)
            {
                data = put;
                stash.Put(data);
            }
            return data;
        }

        public static T GetOrPut<T>(this IStash stash, Func<T> put)
            where T : class
        {
            if (put == null)
                throw new ArgumentNullException(nameof(put));
            var data = stash.TryGet<T>();
            if (data == null)
            {
                data = put();
                stash.Put(data);
            }
            return data;
        }

        public static async Task<T> GetOrPut<T>(this IStash stash, Func<Task<T>> put)
            where T : class
        {
            if (put == null)
                throw new ArgumentNullException(nameof(put));
            var data = stash.TryGet<T>();
            if (data == null)
            {
                data = await put();
                stash.Put(data);
            }
            return data;
        }
    }
}
