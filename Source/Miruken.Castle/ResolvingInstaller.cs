namespace Miruken.Castle
{
    using System;
    using Callback;
    using global::Castle.MicroKernel.Registration;

    public class ResolvingInstaller : PluginInstaller
    {
        private Action<ComponentRegistration> _configure;

        public ResolvingInstaller ConfigureResolving(Action<ComponentRegistration> configure)
        {
            _configure += configure;
            return this;
        }

        protected override void InstallPlugin(Plugin plugin)
        {
            var resolving = Classes.FromAssembly(plugin.Assembly)
                .BasedOn(typeof(IResolving))
                .WithServiceFromInterface();
            if (_configure != null)
                resolving.Configure(_configure);
            Container.Register(resolving);
        }
    }
}
