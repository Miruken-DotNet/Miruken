namespace Miruken.Register
{
    using System;
    using System.Collections.Generic;
    using Callback;
    using Callback.Policy;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceFactoryFacade : Handler
    {
        private readonly List<Handler> _services = new List<Handler>();

        public bool HasServices => _services.Count > 0;

        [Provides]
        public object Provide(Inquiry inquiry, IHandler composer)
        {
            var many = inquiry.Many;
            foreach (var service in _services)
            {
                if (!service.Handle(inquiry, many, composer)) continue;
                if (!many) return null;
            }
            return null;
        }

        public void RegisterService(
            IHandlerDescriptorFactory factory, ServiceDescriptor service,
            HandlerDescriptorVisitor visitor = null)
        {
            var serviceType = service.ImplementationType ?? service.ServiceType;

            var instance = service.ImplementationInstance;
            if (instance != null)
            {
                if (serviceType == null)
                    serviceType = instance.GetType();

                VerifyServiceType(serviceType, service);

                var providerType = typeof(ServiceFactory<>.Instance)
                    .MakeGenericType(serviceType);

                factory.RegisterDescriptor(providerType, visitor);

                var handler = (Handler)Activator.CreateInstance(providerType, instance);
                _services.Add(handler);
                return;
            }

            var implementationFactory = service.ImplementationFactory;
            if (implementationFactory != null)
            {
                if (serviceType == null)
                    serviceType = implementationFactory.GetType().GenericTypeArguments[1];

                VerifyServiceType(serviceType, service);

                Type serviceFactoryType;
                switch (service.Lifetime)
                {
                    case ServiceLifetime.Transient:
                        serviceFactoryType = typeof(ServiceFactory<>.Transient);
                        break;
                    case ServiceLifetime.Singleton:
                        serviceFactoryType = typeof(ServiceFactory<>.Singleton);
                        break;
                    case ServiceLifetime.Scoped:
                        serviceFactoryType = typeof(ServiceFactory<>.Scoped);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported lifetime {service.Lifetime}");
                }

                serviceFactoryType = serviceFactoryType.MakeGenericType(serviceType);

                factory.RegisterDescriptor(serviceFactoryType, visitor);

                var handler = (Handler)Activator.CreateInstance(serviceFactoryType, implementationFactory);
                _services.Add(handler);
                return;
            }

            VerifyServiceType(serviceType, service);

            factory.RegisterDescriptor(serviceType, ServiceConfiguration.For(service) + visitor);
        }

        private static void VerifyServiceType(Type serviceType, ServiceDescriptor service)
        {
            if (serviceType == null)
                throw new ArgumentException($"Unable to infer service type from descriptor {service}");

            if (serviceType == typeof(object))
                throw new ArgumentException($"Service type 'object' from descriptor {service} is not valid");
        }
    }
}
