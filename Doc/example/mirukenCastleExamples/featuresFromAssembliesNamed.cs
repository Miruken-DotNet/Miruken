namespace Example.MirukenCastleExamples
{
    using Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using Castle.Windsor;
    using Miruken.Castle;
    using Miruken.Validate.Castle;

    public class FeaturesFromAssembliesNamed
    {
        public IWindsorContainer Container { get; set; }

        public FeaturesFromAssembliesNamed()
        {
            Container = new WindsorContainer();
            Container.Kernel.Resolver.AddSubResolver(
                new CollectionResolver(Container.Kernel, true));

            Container.Install(
                WithFeatures.FromAssembliesNamed(
                    "Example.League",
                    "Example.School"),
                new ConfigurationFactoryInstaller(),
                new ValidationInstaller()
            );
        }
    }
}
