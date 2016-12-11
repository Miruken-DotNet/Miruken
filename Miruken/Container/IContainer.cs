namespace Miruken.Container
{
    public interface IContainer : IStrict
    {
        T        Resolve<T>();

        object   Resolve(object key);

        T[]      ResolveAll<T>();

        object[] ResolveAll(object key);

        void     Release(object component);
    }
}
