namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Callback;
    using Callback.Policy;
    using Error;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using Infrastructure;
    using Map;

    public class HandleFeature : FeatureInstaller
    {
        private FeatureFilter _filter;
        private Action<ComponentRegistration> _configureHandlers;
        private Action<ComponentRegistration> _configureFilters;

        public HandleFeature SelectHandlers(FeatureFilter filter)
        {
            _filter += filter;
            return this;
        }

        public HandleFeature ConfigureHandlers(Action<ComponentRegistration> configure)
        {
            _configureHandlers += configure;
            return this;
        }

        public HandleFeature ConfigureFiltersFeature(Action<ComponentRegistration> configure)
        {
            _configureFilters += configure;
            return this;
        }

        public override IEnumerable<FromDescriptor> GetFeatures()
        {
            yield return Types.From(typeof(ErrorsHandler), typeof(MappingHandler));
        }

        protected override void Install(IConfigurationStore store)
        {
            base.Install(store);
            var constrainedFilter = FilterSelectorHook.Instance;
            Container.Kernel.AddHandlerSelector(constrainedFilter);
            Container.Kernel.AddHandlersFilter(constrainedFilter);
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
                        _configureHandlers?.Invoke(handler);
                        HandlerDescriptor.GetDescriptor(handler.Implementation);
                    });
                }
            }

            from.BasedOn(typeof(IFilter<,>))
                .WithServiceBase().WithServiceSelf()
                .Configure(filter =>
                {
                    _configureFilters?.Invoke(filter);
                    var impl = filter.Implementation;
                    if (impl.IsGenericType)
                    {
                        var constraints = FilterSelectorHook
                            .GetFilterConstraints(impl);
                        if (constraints != null && constraints.Length > 0)
                            filter.ExtendedProperties(
                                Property.ForKey<FilterSelectorHook>()
                                .Eq(constraints));
                        filter.ExtendedProperties(Property.ForKey(
                            Constants.GenericImplementationMatchingStrategy)
                                .Eq(FilterGenericsHook.Instance));
                    }
                });
        }

        private static IEnumerable<BasedOnDescriptor> SelectDefault(FromDescriptor descriptor)
        {
            yield return descriptor.BasedOn<IResolving>()
                .WithServiceFromInterface()
                .WithServiceSelf();
            yield return descriptor.Where(
                type => (RuntimeHelper.Is<IHandler>(type) ||
                         type.Name.EndsWith("Handler")) &&
                         !type.IsDefined(typeof(UnmanagedAttribute), false))
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
