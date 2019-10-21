namespace Miruken.Register
{
    using System;
    using System.Collections.Generic;
    using Callback;
    using Callback.Policy;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceFactoryFacade : Handler
    {
        private readonly Dictionary<Type, HashSet<Handler>> _services 
            = new Dictionary<Type, HashSet<Handler>>();

        public bool HasServices => _services.Count > 0;

        [Provides]
        public object Provide(Inquiry inquiry, IHandler composer)
        {
            if (!(inquiry.Key is Type serviceType)) return null;

            var many = inquiry.Many;

            HashSet<Handler> services = null;
            if (!many && _services.TryGetValue(serviceType, out services))
            {
                foreach (var service in services)
                    if (service.Handle(inquiry, false, composer)) return null;
            }

            foreach (var serviceGroup in _services)
            foreach (var service in serviceGroup.Value)
            {
                if (serviceType.IsAssignableFrom(serviceGroup.Key) &&
                    services?.Contains(service) != true)
                {
                    if (!service.Handle(inquiry, many, composer)) continue;
                    if (!many) return null;
                }
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

                CheckServiceType(serviceType, service);

                var providerType = typeof(ServiceFactory<>.Instance)
                    .MakeGenericType(serviceType);

                factory.RegisterDescriptor(providerType, visitor);

                var handler = (Handler)Activator.CreateInstance(providerType, instance);
                AddService(serviceType, handler);
                return;
            }

            var implementationFactory = service.ImplementationFactory;
            if (implementationFactory != null)
            {
                if (serviceType == null)
                    serviceType = implementationFactory.GetType().GenericTypeArguments[1];

                CheckServiceType(serviceType, service);

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
                AddService(serviceType, handler);   
                return;
            }

            CheckServiceType(serviceType, service);

            factory.RegisterDescriptor(serviceType, ServiceConfiguration.For(service) + visitor);
        }

        private void AddService(Type serviceType, Handler service)
        {
            if (!_services.TryGetValue(serviceType, out var services))
            {
                services = new HashSet<Handler>();
                _services.Add(serviceType, services);
            }
            services.Add(service);
        }

        private static void CheckServiceType(Type serviceType, ServiceDescriptor service)
        {
            if (serviceType == null)
                throw new ArgumentException($"Unable to infer service type from descriptor {service}");

            if (serviceType == typeof(object))
                throw new ArgumentException($"Service type 'object' from descriptor {service} is not valid");
        }
    }
}
