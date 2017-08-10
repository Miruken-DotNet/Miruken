namespace Miruken.Castle
{
    using System.Collections.Generic;
    using System.Linq;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;

    public class FeaturesInstaller : IWindsorInstaller
    {
        private readonly List<FeatureInstaller> _features;
        private readonly List<FromDescriptor> _from;

        public FeaturesInstaller(params FeatureInstaller[] features)
        {
            _features = new List<FeatureInstaller>();
            _from     = new List<FromDescriptor>();
            Add(features);
        }

        public FeaturesInstaller Add(params FeatureInstaller[] features)
        {
            _features.AddRange(features);
            return this;
        }

        public FeaturesInstaller Use(params FromDescriptor[] from)
        {
            _from.AddRange(from);
            return this;
        }

        void IWindsorInstaller.Install(
            IWindsorContainer container, IConfigurationStore store)
        {
            if (_features.Count == 0) return;
            var implied = new List<FromDescriptor>();

            foreach (var feature in _features)
            {
                ((IWindsorInstaller)feature).Install(container, store);
                implied.AddRange(feature.GetFeatures());
            }

            foreach (var from in _from.Concat(implied))
            {
                foreach (var feature in _features)
                    feature.InstallFeatures(from);
                container.Register(from);
            }
        }
    }
}
