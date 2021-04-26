namespace Miruken.Register
{
    using System.Linq;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Context;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceConfiguration
    {
        private readonly ServiceDescriptor _serviceDescriptor;
        
        private static readonly LifestyleAttribute Scoped    = new ContextualAttribute();
        private static readonly LifestyleAttribute Singleton = new ContextualAttribute { Rooted = true };

        private ServiceConfiguration(ServiceDescriptor serviceDescriptor)
        {
            _serviceDescriptor = serviceDescriptor;
        }

        private void Configure(HandlerDescriptor descriptor, PolicyMemberBinding binding)
        {
            if (binding.Policy != Provides.Policy) return;

            var lifestyle = GetLifestyle(binding);
            if (lifestyle != null)
            {
                if (lifestyle is SingletonAttribute)
                {
                    binding.RemoveFilters(lifestyle);
                    binding.AddFilters(Singleton);
                }
            }
            else if (binding.Dispatcher.IsConstructor)
            {
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

        public static HandlerDescriptorVisitor For(ServiceDescriptor descriptor) =>
            new ServiceConfiguration(descriptor).Configure;

        private static LifestyleAttribute GetLifestyle(IFiltered binding) =>
            binding.Filters.OfType<LifestyleAttribute>().FirstOrDefault();
    }
}

