namespace Miruken.Castle
{
    using System;
    using System.Reflection;
    using Callback;
    using Callback.Policy;
    using global::Castle.MicroKernel.Registration;

    public class HandlerInstaller : FeatureInstaller
    {
        private Func<FromAssemblyDescriptor, BasedOnDescriptor> _selector;
        private Action<ComponentRegistration> _configure;

        public HandlerInstaller Resolving()
        {
            SelectHandlers(SelectResolving);
            return this;
        }

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
            var selector = _selector ?? SelectDefault;
            var basedOn  = selector(handlers);
            if (_configure != null)
                basedOn.Configure(_configure);
            basedOn.Configure(handler => HandlerDescriptor
                .GetDescriptor(handler.Implementation));
            Container.Register(basedOn);
        }

        private static BasedOnDescriptor SelectDefault(FromAssemblyDescriptor descriptor)
        {
            return descriptor.Where(type => 
                typeof(IHandler).IsAssignableFrom(type) || type.Name.EndsWith("Handler"))
                .WithServiceSelf();
        }

        private static BasedOnDescriptor SelectResolving(FromDescriptor descriptor)
        {
            return descriptor.BasedOn<IResolving>()
                .WithServiceFromInterface()
                .WithServiceSelf();
        }
    }
}
