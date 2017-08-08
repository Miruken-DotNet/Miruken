namespace Miruken.Castle
{
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;
    using global::Castle.Windsor.Installer;

    public class WithFeatures : IWindsorInstaller
    {
        private readonly FromDescriptor[] _from;

        public WithFeatures(params FromDescriptor[] from)
        {
            _from = from;
        }

        void IWindsorInstaller.Install(
            IWindsorContainer container, IConfigurationStore store)
        {
            var featureInstallers = container.ResolveAll<FeatureInstaller>();
            if (featureInstallers.Length == 0) return;

            foreach (var from in _from)
            {
                foreach (var featureInstaller in featureInstallers)
                    featureInstaller.InstallFeatures(from);
                container.Register(from);
            }
        }

        public static WithFeatures From(params FromDescriptor[] from)
        {
            return new WithFeatures(from);
        }
    }
}
