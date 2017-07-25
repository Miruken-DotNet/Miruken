namespace Miruken.Castle
{
    using System;
    using System.Reflection;
    using Callback;
    using Callback.Policy;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;

    public class HandlerInstaller : FeatureInstaller
    {
        private Func<FromAssemblyDescriptor, BasedOnDescriptor> _selector;
        private Action<ComponentRegistration> _configure;
        private HandlerLoader _loader;
        private bool _background;

        public HandlerInstaller SelectHandlers(
            Func<FromAssemblyDescriptor, BasedOnDescriptor> selector)
        {
            _selector += selector;
            return this;
        }

        public HandlerInstaller InBackground()
        {
            _background = true;
            return this;
        }

        protected override void Install(IConfigurationStore store)
        {
            if (_background)
            {
                Container.Register(Component.For<HandlerLoader>());
                _loader = Container.Resolve<HandlerLoader>();
            }
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
            basedOn.Configure(handler =>
            {
                var type = handler.Implementation;
                if (_loader != null)
                    _loader.LoadHandler(type);
                else
                    HandlerDescriptor.GetDescriptor(type);

            });
            Container.Register(handlers);
        }

        private static BasedOnDescriptor DefaultSelection(FromAssemblyDescriptor descriptor)
        {
            return descriptor.Where(type => 
                typeof(IHandler).IsAssignableFrom(type) || type.Name.EndsWith("Handler"))
                .WithServiceSelf();
        }
    }
}
