namespace Miruken.Register
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Callback;
    using Callback.Policy.Bindings;
    using Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Scrutor;

    public class Registration
    {
        private SourceSelector _sources;
        private SourceSelector _publicSources;
        private TypeSelector _select;
        private Predicate<Type> _exclude;
        private readonly IServiceCollection _services;

        public delegate IImplementationTypeSelector SourceSelector(ITypeSourceSelector source);
        public delegate void TypeSelector(IImplementationTypeSelector source, bool publicOnly);

        public Registration() : this(null)
        {
        }

        public Registration(IServiceCollection services)
        {
            _services = services ?? new ServiceCollection();
        }

        public Registration Services(Action<IServiceCollection> services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            services(_services);
            return this;
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

        public IServiceCollection Register()
        {
            if (_sources != null || _publicSources != null)
            {
                _services.Scan(scan =>
                {
                    foreach (var source in GetSources())
                    {
                        var from = source.Item1(scan);
                        from.AddClasses(cls => cls.Where(
                            type => !ShouldExclude(type) &&
                            (type.Is<IHandler>() || type.Is<IFilter>() ||
                             type.Is<IResolving>() || type.Name.EndsWith("Handler"))
                        ), source.Item2).AsSelf().WithSingletonLifetime();

                        if (_select != null)
                        {
                            foreach (var select in _select.GetInvocationList())
                                ((TypeSelector)select)(from, source.Item2);
                        }
                    }
                });
            }

            return _services;
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
    }
}
