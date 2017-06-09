namespace Miruken.Tests.Container
{
    using System;
    using Miruken.Callback;
    using Miruken.Concurrency;
    using Miruken.Container;

    public class TestContainer : Handler, IContainer
    {
        T IContainer.Resolve<T>()
        {
            return (T)((IContainer)this).Resolve(typeof(T));
        }

        object IContainer.Resolve(object key)
        {
            var resolution = new Inquiry(key);
            return this.Handle(resolution, false, Composer)
                 ? resolution.Result
                 : Unhandled<object>();
        }

        Promise<T> IContainer.ResolveAsync<T>()
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.Resolve<T>());
        }

        Promise IContainer.ResolveAsync(object key)
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.Resolve(key));
        }

        T[] IContainer.ResolveAll<T>()
        {
            return Array.Empty<T>();
        }

        object[] IContainer.ResolveAll(object key)
        {
            return Array.Empty<object>();
        }

        Promise<T[]> IContainer.ResolveAllAsync<T>()
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.ResolveAll<T>());
        }

        Promise IContainer.ResolveAllAsync(object key)
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.ResolveAll(key));
        }

        void IContainer.Release(object component)
        {
        }

        [Provides]
        private object Resolve(Inquiry inquiry, IHandler composer)
        {
            var type = inquiry.Key as Type;
            return type == null ? Unhandled<object>() : Activator.CreateInstance(type);
        }
    }
}
