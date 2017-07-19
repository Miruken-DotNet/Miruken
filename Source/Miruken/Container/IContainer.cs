namespace Miruken.Container
{
    using Concurrency;

    public interface IContainer
    {
        T                 Resolve<T>();
        object            Resolve(object key);
        Promise<T>        ResolveAsync<T>();
        Promise           ResolveAsync(object key);
        T[]               ResolveAll<T>();
        object[]          ResolveAll(object key);
        Promise<T[]>      ResolveAllAsync<T>();
        Promise<object[]> ResolveAllAsync(object ke);
        void              Release(object component);
    }
}
