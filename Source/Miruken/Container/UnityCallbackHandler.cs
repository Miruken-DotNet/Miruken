using System;
using System.Collections.Generic;
using CompactUnity.Container;
using Miruken.Callback;

namespace Miruken.Container
{
    public class UnityCallbackHandler : CallbackHandler, IContainer, IDisposable
    {
        private readonly UnityContainer _container;
        private readonly Dictionary<object, ObjectPool> _pools;

        public UnityCallbackHandler() 
            : this(new UnityContainer())
        {
        }

        public UnityCallbackHandler(UnityContainer container)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            _container = container;
            _pools     = new Dictionary<object, ObjectPool>();
        }

        public IUnityContainer Container
        {
            get { return _container; }
        }

        T IContainer.Resolve<T>()
        {
            return (T)((IContainer) this).Resolve(typeof(T));
        }

        object IContainer.Resolve(object key)
        {
            var type = key as Type;
            try
            {
                if (type != null)
                {
                    var component = typeof(IRecycling).IsAssignableFrom(type)
                                  ? GetPool(type).Request(() => CreateComponent(type))
                                  : CreateComponent(type);
                    if (component != null)
                    {
                        ComponentCreated(component, Composer);
                        return component;
                    }
                }
            }
            catch (UnityException)
            {
            }
            return Unhandled<object>();
        }

        T[] IContainer.ResolveAll<T>()
        {
            var result = new IContainer(Composer).Resolve<T>();
            return !Equals(result, null) ? new [] { result } : Unhandled<T[]>();
        }

        object[] IContainer.ResolveAll(object key)
        {
            var result = new IContainer(Composer).Resolve(key);
            return !Equals(result, null) ? new[] { result } : Unhandled<object[]>();
        }

        void IContainer.Release(object component)
        {
            try
            {
                if (ReleaseComponent(component))
                    ComponentReleased(component, Composer);
            }
            catch
            {
            }
        }

        [Provides]
        private object Resolve(Resolution resolution)
        {
            var type = resolution.Key as Type;
            // Unity will create objects even if not registered
            return type != null && _container.IsRegistered(type)
                 ? ((IContainer) this).Resolve(resolution.Key)
                 : null;
        }

        protected virtual void ComponentCreated(object component, ICallbackHandler composer)
        {
        }

        protected virtual void ComponentReleased(object component, ICallbackHandler composer)
        {
            var disposable = component as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }

        private object CreateComponent(Type type)
        {
            return _container.Resolve(type);
        }

        private bool ReleaseComponent(object component)
        {
            if (!(component is IRecycling)) return true;
            foreach (var pool in _pools)
            {
                var release = pool.Value.Release(component);
                if (release.HasValue) return release.Value;
            }
            return true;
        }

        private ObjectPool GetPool(object key)
        {
            ObjectPool pool;
            if (_pools.TryGetValue(key, out pool))
                return pool;
            pool = new ObjectPool(1);
            _pools.Add(key, pool);
            return pool;
        }

        public void Dispose()
        {
            foreach (var pool in _pools)
                pool.Value.Dispose();
        }
    }
}
