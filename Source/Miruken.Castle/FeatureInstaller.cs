namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;

    public delegate IEnumerable<BasedOnDescriptor> FeatureFilter(FromDescriptor from);

    public abstract class FeatureInstaller : IWindsorInstaller
    {
        protected IWindsorContainer Container { get; private set; }

        public abstract void InstallFeatures(FromDescriptor from);

        protected virtual void Install(IConfigurationStore store)
        {
        }

        void IWindsorInstaller.Install(IWindsorContainer container, IConfigurationStore store)
        {
            try
            {
                container.Register(Component.For<FeatureInstaller>().Instance(this));
            }
            catch
            {
                return;  // already installed
            }

            Container = container;
            Install(store);
        }
    }
}
