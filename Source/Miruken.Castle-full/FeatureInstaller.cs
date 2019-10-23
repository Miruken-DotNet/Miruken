namespace Miruken.Castle
{
    using System.Collections.Generic;
    using System.Linq;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;

    public delegate IEnumerable<BasedOnDescriptor> FeatureFilter(FromDescriptor from);

    public abstract class FeatureInstaller : IWindsorInstaller
    {
        protected IWindsorContainer Container { get; private set; }

        public abstract void InstallFeatures(FromDescriptor from);

        public virtual IEnumerable<FromDescriptor> GetFeatures()
        {
            return Enumerable.Empty<FromDescriptor>();
        }

        protected virtual void Install(IConfigurationStore store)
        {
        }

        void IWindsorInstaller.Install(IWindsorContainer container, IConfigurationStore store)
        {
            Container = container;
            Install(store);
        }
    }
}
