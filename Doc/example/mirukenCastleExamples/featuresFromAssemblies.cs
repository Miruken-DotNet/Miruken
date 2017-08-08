namespace Example.MirukenCastleExamples
{
    using Castle.MicroKernel.Registration;
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
                new ConfigurationFactoryInstaller(),
                new ValidationInstaller(),
                WithFeatures.From(
                    Classes.FromAssemblyContaining<CreateTeam>(),
                    Classes.FromAssemblyContaining<CreateStudent>())
            );
        }
    }
}
