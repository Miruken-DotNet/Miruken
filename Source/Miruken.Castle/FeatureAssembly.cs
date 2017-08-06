namespace Miruken.Castle
{
    using System.Reflection;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;
    using global::Castle.Windsor.Installer;

    public class FeatureAssembly : IWindsorInstaller
    {
        public FeatureAssembly(Assembly assembly)
        {
            Assembly = assembly;
        }

        public Assembly Assembly       { get; }
        public bool     SkipInstallers { get; set; }

        void IWindsorInstaller.Install(IWindsorContainer container, IConfigurationStore store)
        {
            var name = Assembly.FullName;
            if (container.Kernel.HasComponent(name)) return;
            if (!SkipInstallers)
                container.Install(FromAssembly.Instance(Assembly));
            container.Register(Component.For<FeatureAssembly>().Instance(this).Named(name));
        }
    }
}
