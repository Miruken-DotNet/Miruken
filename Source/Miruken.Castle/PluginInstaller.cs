namespace Miruken.Castle
{
    using System.Linq;
    using System.Reflection;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;

    public abstract class PluginInstaller : IWindsorInstaller
    {
        private readonly Assembly[] _referenced;
        protected IWindsorContainer Container { get; private set; }

        protected PluginInstaller(params Assembly[] referenced)
        {
            _referenced = referenced;
        }

        void IWindsorInstaller.Install(IWindsorContainer container, IConfigurationStore store)
        {
            Container = container;
            Install(store);

            container.Kernel.ComponentRegistered += (key, handler) =>
            {
                if (typeof(Plugin).IsAssignableFrom(handler.ComponentModel.Implementation))
                {
                    var plugin = container.Kernel.Resolve<Plugin>(key);
                    if (ShouldInstallPlugin(plugin))
                        InstallPlugin(plugin);
                }
            };

            var plugins = container.ResolveAll<Plugin>();
            foreach (var plugin in plugins.Where(ShouldInstallPlugin))
                InstallPlugin(plugin);
        }

        protected virtual void Install(IConfigurationStore store)
        {
        }

        protected virtual bool ShouldInstallPlugin(Plugin plugin)
        {
            return _referenced == null || _referenced.Length == 0
                || plugin.Assembly.GetReferencedAssemblies().Select(r => r.FullName)
                    .Intersect(_referenced.Select(r => r.FullName)).Any();
        }

        protected abstract void InstallPlugin(Plugin plugin);
    }
}
