namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Callback;
    using Callback.Policy;
    using global::Castle.DynamicProxy.Internal;
    using global::Castle.MicroKernel.Registration;
    using Infrastructure;

    public class HandleFeature : FeatureInstaller
    {
        private FeatureFilter _filter;
        private Action<ComponentRegistration> _configure;

        public HandleFeature SelectHandlers(FeatureFilter filter)
        {
            _filter += filter;
            return this;
        }

        public HandleFeature ConfigureHandlers(Action<ComponentRegistration> configure)
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
            yield return descriptor.BasedOn<IResolving>()
                .WithServiceFromInterface()
                .WithServiceSelf();
            yield return descriptor.Where(
                type => type.Is<IHandler>() || type.Name.EndsWith("Handler"))
                .WithServiceSelect(HandlerInterfaces)
                .WithServiceSelf();
        }

        private static IEnumerable<Type> HandlerInterfaces(Type type, Type[] baseTypes)
        {
            return type.GetToplevelInterfaces().Except(IgnoredHandlerServices);
        }

        private static readonly Type[] IgnoredHandlerServices =
            { typeof(IHandler), typeof(IProtocolAdapter), typeof(IServiceProvider) };
    }
}
