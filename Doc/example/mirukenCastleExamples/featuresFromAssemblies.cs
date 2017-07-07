namespace Example.MirukenCastleExamples
{
    using Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using Castle.Windsor;
    using League;
    using Miruken.Castle;
    using Miruken.Validate.Castle;
    using School;

    public class FeaturesFromAssemblies
    {
        public IWindsorContainer Container { get; set; }

        public FeaturesFromAssemblies()
        {
            Container = new WindsorContainer();
            Container.Kernel.Resolver.AddSubResolver(
                new CollectionResolver(Container.Kernel, true));

            Container.Install(
                WithFeatures.FromAssemblies(
                    typeof(CreateTeam).Assembly,
                    typeof(CreateStudent).Assembly),
                new ConfigurationFactoryInstaller(),
                new ValidationInstaller()
            );
        }
    }
}
