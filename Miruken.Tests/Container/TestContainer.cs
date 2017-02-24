namespace Miruken.Tests.Container
{
    using System;
    using Miruken.Callback;
    using Miruken.Container;

    public class TestContainer : Handler, IContainer
    {
        T IContainer.Resolve<T>()
        {
            return (T)((IContainer)this).Resolve(typeof(T));
        }

        object IContainer.Resolve(object key)
        {
            var resolution = new Resolution(key);
            return Handle(resolution, false, Composer)
                 ? resolution.Result
                 : Unhandled<object>();
        }

        T[] IContainer.ResolveAll<T>()
        {
            return new T[0];
        }

        object[] IContainer.ResolveAll(object key)
        {
            return new object[0];
        }

        void IContainer.Release(object component)
        {
        }

        [Provides]
        private object Resolve(Resolution resolution, IHandler composer)
        {
            var type = resolution.Key as Type;
            return type == null ? Unhandled<object>() : Activator.CreateInstance(type);
        }
    }
}
