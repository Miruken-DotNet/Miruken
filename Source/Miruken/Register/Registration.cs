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
        private SourceSelector _from;
        private SourceSelector _fromPublic;
        private TypeSelector _select;
        private Predicate<Type> _exclude;
        private readonly IServiceCollection _services;

        public delegate IImplementationTypeSelector SourceSelector(ITypeSourceSelector source);
        public delegate void TypeSelector(IImplementationTypeSelector from, bool publicOnly);

        public Registration() : this(null)
        {
        }

        public Registration(IServiceCollection services)
        {
            _services = services ?? new ServiceCollection();
        }

        public Registration From(params SourceSelector[] from)
        {
            foreach (var source in from)
                _from += source;
            return this;
        }

        public Registration FromPublic(params SourceSelector[] from)
        {
            foreach (var source in from)
                _fromPublic += source;
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
            if (_from != null || _fromPublic != null)
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
            if (_from != null)
            {
                foreach (var from in _from.GetInvocationList())
                    yield return Tuple.Create((SourceSelector)from, false);
            }

            if (_fromPublic != null)
            {
                foreach (var from in _fromPublic.GetInvocationList())
                    yield return Tuple.Create((SourceSelector)from, true);
            }
        }
    }
}
