namespace Miruken.Register
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Context;
    using Microsoft.Extensions.DependencyInjection;

    public class Configuration
    {
        private readonly ServiceDescriptor _descriptor;
        
        private static readonly ContextualAttribute Scoped    = new ContextualAttribute();
        private static readonly SingletonAttribute  Singleton = new SingletonAttribute();

        private Configuration(ServiceDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        private void Configure(HandlerDescriptor descriptor, PolicyMemberBinding binding)
        {
            if (binding.Dispatcher.IsStatic &&
                binding.Dispatcher.Member is ConstructorInfo)
            {
                var lifestyle = GetLifestyle(binding);
                if (lifestyle != null) return;
                switch (_descriptor.Lifetime)
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
            return new Configuration(descriptor).Configure;
        }

        private static Type GetLifestyle(PolicyMemberBinding binding)
        {
            return binding.Filters.OfType<LifestyleAttribute>()
                .Select(provider => provider.LifestyleType)
                .FirstOrDefault();
        }
    }
}
