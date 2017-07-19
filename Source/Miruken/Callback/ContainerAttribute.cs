namespace Miruken.Callback
{
    using Concurrency;
    using Container;

    public class ContainerAttribute : ResolvingAttribute
    {
        protected override object Resolve(object key, IHandler handler)
        {
            return handler.P<IContainer>().Resolve(key);
        }

        protected override Promise ResolveAsync(object key, IHandler handler)
        {
            return handler.P<IContainer>().ResolveAsync(key);
        }

        protected override object[] ResolveAll(object key, IHandler handler)
        {
            return handler.P<IContainer>().ResolveAll(key);
        }

        protected override Promise<object[]> ResolveAllAsync(object key, IHandler handler)
        {
            return handler.P<IContainer>().ResolveAllAsync(key);
        }
    }
}
