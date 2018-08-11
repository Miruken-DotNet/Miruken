namespace Miruken.Callback
{
    using Concurrency;
    using Container;

    public class ContainerAttribute : ResolvingAttribute
    {
        protected override object Resolve(Inquiry parent,
            object key, IHandler handler, IHandler composer)
        {
            return handler.Proxy<IContainer>().Resolve(key);
        }

        protected override Promise ResolveAsync(Inquiry parent,
            object key, IHandler handler, IHandler composer)
        {
            return handler.Proxy<IContainer>().ResolveAsync(key);
        }

        protected override object[] ResolveAll(Inquiry parent,
            object key, IHandler handler, IHandler composer)
        {
            return handler.Proxy<IContainer>().ResolveAll(key);
        }

        protected override Promise<object[]> ResolveAllAsync(
            Inquiry parent, object key, IHandler handler, IHandler composer)
        {
            return handler.Proxy<IContainer>().ResolveAllAsync(key);
        }
    }
}
