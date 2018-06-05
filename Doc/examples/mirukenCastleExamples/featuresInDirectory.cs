namespace Example.mirukenCastleExamples
{
    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using Castle.Windsor;
    using Miruken.Castle;
    using Miruken.Validate.Castle;

    public class FeaturesInDirectory
    {
        public IWindsorContainer Container { get; set; }

        public FeaturesInDirectory()
        {
            Container = new WindsorContainer();
            Container.Kernel.Resolver.AddSubResolver(
                new CollectionResolver(Container.Kernel, true));

            Container.Install(
                new FeaturesInstaller(
                    new ConfigurationFeature(), new ValidateFeature())
                    .Use(Classes.FromAssemblyInDirectory(new AssemblyFilter("")
                        .FilterByName(x => x.Name.StartsWith("Example."))))
            );
        }
    }
}
