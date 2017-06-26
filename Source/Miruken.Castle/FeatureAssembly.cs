namespace Miruken.Castle
{
    using System;
    using System.Reflection;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;
    using From = global::Castle.Windsor.Installer.FromAssembly;

    public class FeatureAssembly : IWindsorInstaller, IEquatable<FeatureAssembly>
    {
        public FeatureAssembly(Assembly assembly)
        {
            Assembly = assembly;
        }

        public Assembly Assembly { get; }

        void IWindsorInstaller.Install(IWindsorContainer container, IConfigurationStore store)
        {
            var name = Assembly.FullName;
            if (container.Kernel.HasComponent(name)) return;
            container.Install(From.Instance(Assembly));
            container.Register(Component.For<FeatureAssembly>().Instance(this).Named(name));
        }

        public bool Equals(FeatureAssembly other)
        {
            if (other == null) return false;
            return ReferenceEquals(this, other) || Equals(Assembly, other.Assembly);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FeatureAssembly);
        }

        public override int GetHashCode()
        {
            return Assembly.GetHashCode();
        }
    }
}
