namespace Miruken.Castle
{
    using System;
    using System.Reflection;
    using Callback;
    using global::Castle.MicroKernel.Registration;

    public class ResolvingInstaller : FeatureInstaller
    {
        private Action<ComponentRegistration> _configure;

        public ResolvingInstaller ConfigureResolving(Action<ComponentRegistration> configure)
        {
            _configure += configure;
            return this;
        }

        protected override void InstallFeature(Assembly assembly)
        {
            var resolving = Classes.FromAssembly(assembly)
                .BasedOn(typeof(IResolving))
                .WithServiceFromInterface();
            if (_configure != null)
                resolving.Configure(_configure);
            Container.Register(resolving);
        }
    }
}
