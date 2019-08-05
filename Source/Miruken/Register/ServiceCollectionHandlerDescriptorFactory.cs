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
                if (descriptor.ImplementationInstance != null ||
                    descriptor.ImplementationFactory != null) continue;

                var implementationType = descriptor.ImplementationType;

                if (implementationType == null || implementationType == typeof(object))
                {
                    throw new ArgumentException(
                        $"Unable to infer service type from {descriptor}");
                }

                RegisterDescriptor(implementationType, ServiceConfiguration.For(descriptor));
            }
        }
    }
}
