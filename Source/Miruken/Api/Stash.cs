namespace Miruken.Api
{
    using System;
    using System.Collections.Generic;
    using Callback;

    [Unmanaged]
    public class Stash : Handler
    {
        private readonly bool _root;
        private readonly Dictionary<Type, object> _data;

        public Stash(bool root = false)
        {
            _root = root;
            _data = new Dictionary<Type, object>();
        }

        [Provides(Strict = true)]
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

        [Handles]
        public object Get(StashAction.Get get)
        {
            if (_data.TryGetValue(get.Type, out var data))
            {
                get.Value = data;
                return true;
            }
            return _root ? (object)true : null;
        }

        [Handles]
        public void Put(StashAction.Put put)
        {
            _data[put.Type] = put.Value;
        }

        [Handles]
        public void Drop(StashAction.Drop drop)
        {
            _data.Remove(drop.Type);
        }
    }
}
