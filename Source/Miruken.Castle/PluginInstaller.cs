namespace Miruken.Castle
{
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;

    public abstract class PluginInstaller : IWindsorInstaller
    {
        protected IWindsorContainer Container { get; private set; }

        void IWindsorInstaller.Install(IWindsorContainer container, IConfigurationStore store)
        {
            Container = container;
            Install(store);

            container.Kernel.ComponentRegistered += (key, handler) =>
            {
                if (typeof(Plugin).IsAssignableFrom(handler.ComponentModel.Implementation))
                {
                    var plugin = container.Kernel.Resolve<Plugin>(key);
                    InstallPlugin(plugin);
                }
            };

            var plugins = container.ResolveAll<Plugin>();
            foreach (var plugin in plugins) InstallPlugin(plugin);
        }

        protected virtual void Install(IConfigurationStore store)
        {
        }

        protected abstract void InstallPlugin(Plugin plugin);
    }
}
