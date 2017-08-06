namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using Callback;
    using Callback.Policy;
    using global::Castle.MicroKernel.Registration;

    public class HandlerInstaller : FeatureInstaller
    {
        private FeatureFilter _filter;
        private Action<ComponentRegistration> _configure;

        public HandlerInstaller SelectHandlers(FeatureFilter filter)
        {
            _filter += filter;
            return this;
        }

        public HandlerInstaller ConfigureHandlers(Action<ComponentRegistration> configure)
        {
            _configure += configure;
            return this;
        }

        protected override void InstallFeature(FeatureAssembly feature)
        {
            var selection = _filter ?? SelectDefault;
            var handlers  = Classes.FromAssembly(feature.Assembly);

            foreach (FeatureFilter filter in selection.GetInvocationList())
            {
                var selector = filter(handlers);
                foreach (var basedOn in selector)
                {
                    basedOn.Configure(handler =>
                    {
                        _configure?.Invoke(handler);
                        HandlerDescriptor.GetDescriptor(handler.Implementation);
                    });
                }
            }

            Container.Register(handlers);
        }

        private static IEnumerable<BasedOnDescriptor> SelectDefault(FromDescriptor descriptor)
        {
            yield return descriptor.Where(
                type => !typeof(IResolving).IsAssignableFrom(type) 
                     && (typeof(IHandler).IsAssignableFrom(type) 
                     || type.Name.EndsWith("Handler")));
            yield return descriptor.BasedOn<IResolving>()
                .WithServiceFromInterface()
                .WithServiceSelf();
        }
    }
}
