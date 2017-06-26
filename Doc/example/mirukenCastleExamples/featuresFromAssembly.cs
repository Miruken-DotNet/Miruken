namespace Example.MirukenCastleExamples
{
    using Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using Castle.Windsor;
    using Miruken.Castle;
    using Miruken.Validate.Castle;
    using League;
    using School;

    public class FeaturesFromAssembly
    {
        public IWindsorContainer Container { get; set; }

        public FeaturesFromAssembly()
        {
            Container = new WindsorContainer();
            Container.Kernel.Resolver.AddSubResolver(
                new CollectionResolver(Container.Kernel, true));

            Container.Install(
                Features.FromAssemblies(
                    typeof(CreateTeam).Assembly,
                    typeof(CreateStudent).Assembly),
                new ConfigurationFactoryInstaller(),
                new ValidationInstaller()
            );
        }
    }
}
