namespace Miruken.Register
{
    using System;
    using Callback.Policy;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceCollectionHandlerDescriptorFactory : MutableHandlerDescriptorFactory
    {
        public ServiceCollectionHandlerDescriptorFactory(
            IServiceCollection services,
            HandlerDescriptorVisitor visitor = null) : base(visitor)
        {
            RegisterServices(services);
        }

        public void RegisterServices(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            foreach (var descriptor in services)
            {
                var implementationType = GetImplementationType(descriptor);

                if (implementationType == null || implementationType == typeof(object))
                {
                    throw new ArgumentException(
                        $"Unable to infer service type from {descriptor}");
                }

                RegisterDescriptor(implementationType, ServiceConfiguration.For(descriptor));
            }
        }

        private static Type GetImplementationType(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationType != null)
                return descriptor.ImplementationType;

            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance.GetType();

            if (descriptor.ImplementationFactory != null)
            {
                var typeArguments = descriptor.ImplementationFactory
                    .GetType().GenericTypeArguments;
                if (typeArguments.Length == 2)
                    return typeArguments[1];
            }

            return null;
        }
    }
}
