namespace Miruken.Castle
{
    using System;
    using System.Linq;
    using Callback;
    using global::Castle.Core;
    using global::Castle.MicroKernel;
    using global::Castle.MicroKernel.Context;
    using global::Castle.MicroKernel.Handlers;
    using Infrastructure;

    public class ConstrainedFilterSelector : IHandlerSelector, IHandlersFilter
    {
        public static readonly ConstrainedFilterSelector Instance =
            new ConstrainedFilterSelector();

        private ConstrainedFilterSelector()
        {          
        }

        bool IHandlerSelector.HasOpinionAbout(string key, Type service)
        {
            return ((IHandlersFilter)this).HasOpinionAbout(service);
        }

        bool IHandlersFilter.HasOpinionAbout(Type service)
        {
            return service.GetOpenTypeConformance(typeof(IFilter<,>)) != null;
        }

        global::Castle.MicroKernel.IHandler IHandlerSelector.SelectHandler(
            string key, Type service, global::Castle.MicroKernel.IHandler[] handlers)
        {
            return handlers.FirstOrDefault(h => MatchHandler(service, h));
        }

        global::Castle.MicroKernel.IHandler[] IHandlersFilter.SelectHandlers(
            Type service, global::Castle.MicroKernel.IHandler[] handlers)
        {
            return handlers.Where(h => MatchHandler(service, h)).ToArray();
        }

        public static Type GetFilterConstraint(Type filter)
        {
            if (filter.ContainsGenericParameters)
            {
                var genericArgs = filter.GetGenericArguments();
                if (genericArgs.Length > 0 &&
                    genericArgs[0].GenericParameterPosition == 0)
                {
                    var constraints = genericArgs[0]
                        .GetGenericParameterConstraints();
                    if (constraints.Length == 1)
                        return constraints[0];
                }
            }
            return null;
        }

        private static bool MatchHandler(
            Type service, global::Castle.MicroKernel.IHandler handler)
        {
            var constraint = (Type)handler.ComponentModel
                .ExtendedProperties[typeof(ConstrainedFilterSelector)];
            if (constraint == null) return true;
            var closingTypeActual = service.GetGenericArguments().First();
            return constraint.IsAssignableFrom(closingTypeActual);
        }
    }

    public class ConstrainedFilterMatching : IGenericImplementationMatchingStrategy
    {
        public static readonly ConstrainedFilterMatching Instance =
            new ConstrainedFilterMatching();

        private ConstrainedFilterMatching()
        {          
        }

        public Type[] GetGenericArguments(
            ComponentModel model, CreationContext context)
        {
            if (ConstrainedFilterSelector
                .GetFilterConstraint(model.Implementation) == null)
                return null;
            var actualArgs = context.RequestedType.GetGenericArguments();
            return model.Implementation.GetGenericArguments()
                .Select(arg => actualArgs[arg.GenericParameterPosition])
                .ToArray();
        }
    }
}
