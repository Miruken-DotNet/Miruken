namespace Miruken.Castle
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;

    public delegate IEnumerable<BasedOnDescriptor> FeatureFilter(FromDescriptor from);

    public abstract class FeatureInstaller : IWindsorInstaller
    {
        private readonly Assembly[] _referenced;
        protected IWindsorContainer Container { get; private set; }

        protected FeatureInstaller(params Assembly[] referenced)
        {
            _referenced = referenced;
        }

        void IWindsorInstaller.Install(IWindsorContainer container, IConfigurationStore store)
        {
            Container = container;
            Install(store);

            container.Kernel.ComponentRegistered += (key, handler) =>
            {
                if (typeof(FeatureAssembly).IsAssignableFrom(handler.ComponentModel.Implementation))
                {
                    var feature = container.Kernel.Resolve<FeatureAssembly>(key);
                    if (ShouldInstallFeature(feature))
                        InstallFeature(feature.Assembly);
                }
            };

            var features = container.ResolveAll<FeatureAssembly>();
            foreach (var feature in features.Where(ShouldInstallFeature))
                InstallFeature(feature.Assembly);
        }

        protected virtual void Install(IConfigurationStore store)
        {
        }

        protected virtual bool ShouldInstallFeature(FeatureAssembly feature)
        {
            if (_referenced == null || _referenced.Length == 0)
                return true;
            var assembly = feature.Assembly;
            if (_referenced.Contains(assembly)) return true;
            var references = assembly.GetReferencedAssemblies();
            return _referenced.Any(p => references.Any(r => r.FullName == p.FullName));
        }

        protected abstract void InstallFeature(Assembly assembly);
    }
}
