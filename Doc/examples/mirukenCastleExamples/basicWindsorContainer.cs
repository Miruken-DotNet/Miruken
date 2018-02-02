namespace Example.MirukenCastleExamples
{
    using Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using Castle.Windsor;
    using Castle.Windsor.Installer;

    public class BasicWindsorContainer
    {
        public IWindsorContainer Container { get; set; }

        public BasicWindsorContainer()
        {
            Container = new WindsorContainer();
            Container.Kernel.Resolver.AddSubResolver(
                new CollectionResolver(Container.Kernel, true));
            Container.Install(FromAssembly.This());
        }
    }
}
