namespace Example.MirukenCastleExamples
{
    using Castle.MicroKernel.Registration;
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
                new FeaturesInstaller(
                    new ConfigurationFeature(), new ValidateFeature())
                        .Use(Classes.FromAssemblyNamed("Example.League"),
                             Classes.FromAssemblyNamed("Example.School"))
            );
        }
    }
}
