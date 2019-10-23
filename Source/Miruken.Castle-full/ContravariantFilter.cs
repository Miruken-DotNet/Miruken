namespace Miruken.Castle
{
    using System;
    using System.Linq;
    using System.Reflection;
    using global::Castle.MicroKernel;

    public class ContravariantFilter : IHandlersFilter
    {
        public bool HasOpinionAbout(Type service)
        {
            if (!service.IsGenericType) return false;
            var genericType      = service.GetGenericTypeDefinition();
            var genericArguments = genericType.GetGenericArguments();
            return genericArguments.Count(arg => arg.GenericParameterAttributes
                .HasFlag(GenericParameterAttributes.Contravariant)) == 1;
        }

        public IHandler[] SelectHandlers(Type service, IHandler[] handlers)
        {
            return handlers.OrderBy(h =>
                h.ComponentModel.Implementation.IsGenericTypeDefinition ? 1 : 0)
                .ToArray();
        }
    }
}
