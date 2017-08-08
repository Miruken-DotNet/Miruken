namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using Callback;
    using Callback.Policy;
    using global::Castle.MicroKernel.Registration;
    using Infrastructure;

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

        public override void InstallFeatures(FromDescriptor from)
        {
            var selection = _filter ?? SelectDefault;

            foreach (FeatureFilter filter in selection.GetInvocationList())
            {
                var selector = filter(from);
                foreach (var basedOn in selector)
                {
                    basedOn.Configure(handler =>
                    {
                        _configure?.Invoke(handler);
                        HandlerDescriptor.GetDescriptor(handler.Implementation);
                    });
                }
            }
        }

        private static IEnumerable<BasedOnDescriptor> SelectDefault(FromDescriptor descriptor)
        {
            yield return descriptor.Where(
                type => !type.Is<IResolving>() && (type.Is<IHandler>()
                     || type.Name.EndsWith("Handler")));
            yield return descriptor.BasedOn<IResolving>()
                .WithServiceFromInterface()
                .WithServiceSelf();
        }
    }
}
