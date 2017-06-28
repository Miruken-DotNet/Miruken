namespace Miruken.Castle
{
    using System;
    using System.Reflection;
    using Callback;
    using global::Castle.MicroKernel.Registration;

    public class HandlerInstaller : FeatureInstaller
    {
        private Func<FromAssemblyDescriptor, BasedOnDescriptor> _selector;
        private Action<ComponentRegistration> _configure;

        public HandlerInstaller SelectHandlers(
            Func<FromAssemblyDescriptor, BasedOnDescriptor> selector)
        {
            _selector += selector;
            return this;
        }

        public HandlerInstaller ConfigureHandlers(Action<ComponentRegistration> configure)
        {
            _configure += configure;
            return this;
        }

        protected override void InstallFeature(Assembly assembly)
        {
            var handlers = Classes.FromAssembly(assembly);
            var selector = _selector ?? DefaultSelection;
            var basedOn  = selector(handlers);
            if (_configure != null)
                basedOn.Configure(_configure);
            Container.Register(handlers);
        }

        private static BasedOnDescriptor DefaultSelection(FromAssemblyDescriptor descriptor)
        {
            return descriptor.Where(type => 
                typeof(IHandler).IsAssignableFrom(type)
                || type.Name.EndsWith("Handler"))
                .WithServiceSelf();
        }
    }
}
