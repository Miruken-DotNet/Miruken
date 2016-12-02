using System;
using System.Collections;

namespace Miruken.Container
{
    public class ObjectPool : IDisposable
    {
        private readonly int _maxSize;
        private readonly Stack _available;
        private readonly Hashtable _inUse;

        public ObjectPool(int maxSize)
        {
            _maxSize   = maxSize;
            _available = new Stack();
            _inUse     = new Hashtable();
        }

        public object Request(Func<object> create)
        {
            var available = _available.Count > 0;
            var instance  = available ? _available.Pop() : create();
            if (instance == null) return null;
            if (available)
            {
                var recycle = instance as IRecycling;
                if (recycle != null) recycle.Restore();
            }
            _inUse.Add(instance, null);
            return instance;
        }

        public bool? Release(object instance)
        {
            if (!_inUse.Contains(instance)) return null;
            _inUse.Remove(instance);
            if (_available.Count >= _maxSize) return true;
            var recycle = instance as IRecycling;
            if (recycle != null) recycle.Recycle();
            _available.Push(instance);
            return false;
        }

        public void Dispose()
        {
            foreach (var instance in _available)
                TryDispose(instance);
            foreach (DictionaryEntry entry in _inUse)
                TryDispose(entry.Key);
        }

        private static void TryDispose(object instance)
        {
            var disposable = instance as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }
}
