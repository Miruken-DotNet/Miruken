namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Api.Cache;
    using Api.Oneway;
    using Api.Route;
    using Api.Schedule;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Error;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using Infrastructure;

    public class HandleFeature : FeatureInstaller
    {
        private FeatureFilter _filter;
        private Action<ComponentRegistration> _configureHandlers;
        private Action<ComponentRegistration> _configureFilters;
        private Predicate<Type> _excludeHandlers;

        public HandleFeature SelectHandlers(FeatureFilter filter)
        {
            _filter += filter;
            return this;
        }

        public HandleFeature ExcludeHandlers(Predicate<Type> exclude)
        {
            _excludeHandlers += exclude;
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
            yield return Types.From(
                typeof(CachedHandler),
                typeof(OnewayHandler),
                typeof(PassThroughRouter),
                typeof(Scheduler),
                typeof(ErrorsHandler),
                typeof(LogFilter<,>));
        }

        public HandleFeature AddFilters(params IFilterProvider[] providers)
        {
            Handles.AddFilters(providers);
            return this;
        }

        public HandleFeature AddMethodFilters(params IFilterProvider[] providers)
        {
            HandleMethodBinding.AddGlobalFilters(providers);
            return this;
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
            var selection = _filter ?? DefaultHandlers;

            foreach (FeatureFilter filter in selection.GetInvocationList())
            {
                var selector = filter(from);
                foreach (var basedOn in selector)
                {
                    basedOn.Unless(ExcludeHandler).Configure(handler =>
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

        private bool ExcludeHandler(Type handlerType)
        {
            return _excludeHandlers?.GetInvocationList()
                .Cast<Predicate<Type>>()
                .Any(exclude => exclude(handlerType))
                ?? false;
        }

        public static IEnumerable<BasedOnDescriptor> DefaultHandlers(FromDescriptor descriptor)
        {
            yield return descriptor.BasedOn<IResolving>()
                .WithServiceFromInterface()
                .WithServiceSelf();
            yield return descriptor.Where(
                type => (RuntimeHelper.Is<IHandler>(type) ||
                         type.Name.EndsWith("Handler")) &&
                         !type.IsDefined(typeof(UnmanagedAttribute), true))
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
