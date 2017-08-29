namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Callback;
    using global::Castle.Core;
    using global::Castle.MicroKernel;
    using global::Castle.MicroKernel.Context;
    using global::Castle.MicroKernel.Handlers;
    using Infrastructure;
    using CastleHandler=global::Castle.MicroKernel.IHandler;

    public class FilterSelectorHook : IHandlerSelector, IHandlersFilter
    {
        public static readonly FilterSelectorHook Instance =
            new FilterSelectorHook();

        private FilterSelectorHook()
        {          
        }

        bool IHandlerSelector.HasOpinionAbout(string key, Type service)
        {
            return IsFilter(service);
        }

        bool IHandlersFilter.HasOpinionAbout(Type service)
        {
            return IsFilter(service);
        }

        CastleHandler IHandlerSelector.SelectHandler(
            string key, Type service, CastleHandler[] handlers)
        {
            return handlers.OrderBy(PreferOverride)
                .FirstOrDefault(h => MatchHandler(service, h));
        }

        CastleHandler[] IHandlersFilter.SelectHandlers(
            Type service, CastleHandler[] handlers)
        {
            var genericOverrides = new HashSet<Type>();
            handlers = handlers.OrderBy(PreferOverride).ToArray();
            return handlers.Where(h => MatchHandler(service, h, genericOverrides))
                .ToArray();
        }

        public static Type GetFilterConstraint(Type filter)
        {
            if (filter.ContainsGenericParameters)
            {
                var genericArgs = filter.GetGenericArguments();
                var openFilter  = filter.GetOpenTypeConformance(typeof(IFilter<,>));
                if (Array.IndexOf(openFilter.GetGenericArguments(), genericArgs[0]) == 0)
                {
                    var constraints = genericArgs[0].GetGenericParameterConstraints();
                    if (constraints.Length == 1) return constraints[0];
                }
            }
            return null;
        }

        private static bool IsFilter(Type service)
        {
            return service.GetOpenTypeConformance(typeof(IFilter<,>)) != null;
        }

        private static bool MatchHandler(Type service, CastleHandler handler,
                                         ISet<Type> genericOverrides = null)
        {
            if (genericOverrides != null && service.IsGenericType)
            {
                var openService = service.GetOpenTypeConformance(typeof(IFilter<,>));
                if (!genericOverrides.Add(openService)) return false;
            }
            var constraint = (Type)handler.ComponentModel
                .ExtendedProperties[typeof(FilterSelectorHook)];
            return constraint == null || 
                service.GetGenericArguments()[0].Is(constraint);
        }

        private static int PreferOverride(CastleHandler handler)
        {
            var impl = handler.ComponentModel.Implementation;
            return impl.IsGenericType ? impl.GetGenericArguments().Length : 0;
        }
    }

    public class FilterGenericsHook : IGenericImplementationMatchingStrategy
    {
        public static readonly FilterGenericsHook Instance =
            new FilterGenericsHook();

        private FilterGenericsHook()
        {          
        }

        public Type[] GetGenericArguments(
            ComponentModel model, CreationContext context)
        {
            var filter     = model.Implementation;
            var filterArgs = filter.GetGenericArguments();
            if (filterArgs.Length == 0) return null;
            var actualArgs = context.RequestedType.GetGenericArguments();
            if (filterArgs.Length == 2) return actualArgs;
            var openFilter = filter.GetOpenTypeConformance(typeof(IFilter<,>));
            var openArgs   = openFilter.GetGenericArguments();
            var filterArg  = filterArgs[0];
            for (var i = 0; i < 2; ++i)
            {
                var openArg = openArgs[i];
                if (openArg == filterArg ||
                    openArg.GetGenericArguments().Contains(filterArg))
                    return new[] {actualArgs[i]};
            }
            return null;
        }
    }
}
