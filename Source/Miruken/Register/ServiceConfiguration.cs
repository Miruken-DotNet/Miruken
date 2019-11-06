#if NETSTANDARD
namespace Miruken.Register
{
    using System;
    using System.Linq;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Context;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceConfiguration
    {
        private readonly ServiceDescriptor _serviceDescriptor;
        
        private static readonly ContextualAttribute Scoped    = new ContextualAttribute();
        private static readonly SingletonAttribute  Singleton = new SingletonAttribute();

        private ServiceConfiguration(ServiceDescriptor serviceDescriptor)
        {
            _serviceDescriptor = serviceDescriptor;
        }

        private void Configure(HandlerDescriptor descriptor, PolicyMemberBinding binding)
        {
            if (binding.Dispatcher.IsConstructor)
            {
                var lifestyle = GetLifestyle(binding);
                if (lifestyle != null) return;
                switch (_serviceDescriptor.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        binding.AddFilters(Scoped);
                        break;
                    case ServiceLifetime.Singleton:
                        binding.AddFilters(Singleton);
                        break;
                }
            }
        }

        public static HandlerDescriptorVisitor For(ServiceDescriptor descriptor)
        {
            return new ServiceConfiguration(descriptor).Configure;
        }

        private static Type GetLifestyle(IFiltered binding)
        {
            return binding.Filters.OfType<LifestyleAttribute>()
                .Select(provider => provider.LifestyleType)
                .FirstOrDefault();
        }
    }
}
#endif
