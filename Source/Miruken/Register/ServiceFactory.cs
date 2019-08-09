namespace Miruken.Register
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;

    [Unmanaged]
    public class ServiceFactory<T> : Handler
    {
        [Provides, Singleton]
        public ServiceFactory()
        {    
        }

        [Provides, SkipFilters]
        public T Create() => throw new NotSupportedException(
            $"This should not happen if {nameof(ServiceFactoryProvider)} was added to filters");
    }

    public class ServiceFactoryProvider : IFilterProvider
    {
        public ServiceFactoryProvider(Func<IServiceProvider, object> factory)
        {
            Factory = factory
                ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool Required { get; } = true;

        public Func<IServiceProvider, object> Factory { get; }

        IEnumerable<IFilter> IFilterProvider.GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            object callback, Type callbackType, IHandler composer)
        {
            var logicalReturnType = dispatcher.LogicalReturnType;
            var provider = Providers.GetOrAdd(logicalReturnType, rt =>
                (IFilter)Activator.CreateInstance(
                    typeof(InstanceFilter<>).MakeGenericType(rt)));
            return new[] { provider };
        }

        private class InstanceFilter<T> : IFilter<Inquiry, T>
        {
            public int? Order { get; set; } = int.MaxValue - 1;

            public Task<T> Next(Inquiry callback,
                object rawCallback, MemberBinding member,
                IHandler composer, Next<T> next,
                IFilterProvider provider)
            {
                var factory = ((ServiceFactoryProvider)provider).Factory;
                return Task.FromResult((T)factory(composer));
            }
        }

        private static readonly ConcurrentDictionary<Type, IFilter>
            Providers = new ConcurrentDictionary<Type, IFilter>();
    }
}
