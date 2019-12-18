#if NETSTANDARD
namespace Miruken.Register
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Api;
    using Api.Cache;
    using Api.Once;
    using Api.Oneway;
    using Api.Route;
    using Api.Schedule;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Context;
    using Error;
    using Infrastructure;
    using Log;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Scrutor;

    public class Registration
    {
        private SourceSelector _sources;
        private SourceSelector _publicSources;
        private TypeSelector _select;
        private Predicate<Type> _exclude;
        private readonly IServiceCollection _explicitServices;
        private readonly IServiceCollection _implicitServices;
        private readonly HashSet<object> _keys;

        public delegate IImplementationTypeSelector SourceSelector(ITypeSourceSelector source);
        public delegate void TypeSelector(IImplementationTypeSelector selector, bool publicOnly);

        public Registration() : this(null)
        {
        }

        public Registration(IServiceCollection services)
        {
            _explicitServices = services ?? new ServiceCollection();
            _implicitServices = new ServiceCollection();
            _keys             = new HashSet<object>();

            _explicitServices.AddSingleton(this);
        }

        public IEnumerable<IHandler> Handlers { get; } = new List<IHandler>();

        public bool CanRegister(object key)
        {
            return _keys.Add(key);
        }

        public Registration Sources(params SourceSelector[] sources)
        {
            foreach (var source in sources)
                _sources += source;
            return this;
        }

        public Registration PublicSources(params SourceSelector[] sources)
        {
            foreach (var source in sources)
                _publicSources += source;
            return this;
        }

        public Registration Select(params TypeSelector[] selectors)
        {
            foreach (var selector in selectors)
                _select += selector;
            return this;
        }

        public Registration Exclude(params Predicate<Type>[] excludes)
        {
            foreach (var exclude in excludes)
                _exclude += exclude;
            return this;
        }

        public Registration Services(Action<IServiceCollection> services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            services(_explicitServices);
            return this;
        }

        public Registration AddHandlers(params IHandler[] handlers)
        {
            ((List<IHandler>)Handlers).AddRange(handlers);
            return this;
        }

        public Registration With(object value)
        {
            return AddHandlers(new Provider(value));
        }

        public Registration AddFilters(params IFilterProvider[] providers)
        {
            Handles.AddFilters(providers);
            return this;
        }

        public Registration AddMethodFilters(params IFilterProvider[] providers)
        {
            HandleMethodBinding.AddGlobalFilters(providers);
            return this;
        }

        public Registration TrackSingletons()
        {
            SingletonTracker.Track = true;
            return this;
        }

        public Context Build(IHandlerDescriptorFactory factory = null)
        {
            _explicitServices.RemoveAll<Registration>();

            factory ??= new MutableHandlerDescriptorFactory
            {
                ImplicitLifestyle = null
            };

            if (_sources != null || _publicSources != null)
            {
                _implicitServices.Scan(scan =>
                {
                    foreach (var source in GetSources())
                    {
                        var from = source.Item1(new TypeSourceSelectorWrapper(scan));
                        from.AddClasses(cls => cls.Where(
                            type => !ShouldExclude(type) &&
                            (type.Is<IHandler>() || type.Is<IFilter>() ||
                             type.Is<IResolving>() || type.Name.EndsWith("Handler"))
                        ), source.Item2)
                            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                            .AsSelf().WithSingletonLifetime();

                        if (_select != null)
                        {
                            foreach (var select in _select.GetInvocationList())
                                ((TypeSelector)select)(from, source.Item2);
                        }
                    }
                });
            }

            foreach (var service in AddDefaultServices(_implicitServices))
            {
                var serviceType = service.ImplementationType ?? service.ServiceType;
                var visitor     = !serviceType.IsDefined(typeof(UnmanagedAttribute), true)
                                ? ServiceConfiguration.For(service)
                                : null;
                factory.RegisterDescriptor(serviceType, visitor);
            }

            var context = new Context();
            context.AddHandlers(Handlers);

            var serviceFacade = new ServiceFactoryFacade(_explicitServices, factory);
            if (serviceFacade.HasServices) context.AddHandlers(serviceFacade);

            context.AddHandlers((new StaticHandler() + new Stash(true)).Break());

            HandlerDescriptorFactory.UseFactory(factory);

            if (SingletonTracker.Track)
                context.ContextEnded += (ctx, _) => SingletonTracker.Dispose();

            return context;
        }

        private bool ShouldExclude(Type serviceType)
        {
            return _exclude?.GetInvocationList()
                       .Cast<Predicate<Type>>()
                       .Any(exclude => exclude(serviceType))
                ?? false;
        }

        private IEnumerable<Tuple<SourceSelector, bool>> GetSources()
        {
            if (_sources != null)
            {
                foreach (var source in _sources.GetInvocationList())
                    yield return Tuple.Create((SourceSelector)source, false);
            }

            if (_publicSources != null)
            {
                foreach (var source in _publicSources.GetInvocationList())
                    yield return Tuple.Create((SourceSelector)source, true);
            }
        }

        private static IServiceCollection AddDefaultServices(IServiceCollection services)
        {
            services.AddTransient<Provider>();
            services.AddTransient<ServiceProvider>();
            services.AddTransient<ServiceFactoryFacade>();
            services.AddTransient<BatchRouter>();
            services.AddTransient<Stash>();

            services.AddSingleton<ErrorsHandler>();
            services.AddSingleton<CachedHandler>();
            services.AddSingleton<OnewayHandler>();
            services.AddSingleton<OnceHandler>();
            services.AddSingleton<PassThroughRouter>();
            services.AddSingleton<Scheduler>();

            services.AddSingleton<LoggerProvider>();
            services.AddSingleton(typeof(LogFilter<,>));

            return services;
        }
    }
}
#endif