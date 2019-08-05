namespace Miruken.Register
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;

    public class InstanceProvider<T> : IFilter<Inquiry, T>
    {
        public int? Order { get; set; } = int.MaxValue - 1;

        public Task<T> Next(Inquiry callback,
            object rawCallback, MemberBinding member,
            IHandler composer, Next<T> next,
            IFilterProvider provider)
        {
            var factory = ((InstanceProviderProvider) provider).Factory;
            return Task.FromResult((T) factory(composer));
        }
    }

    public class InstanceProviderProvider : IFilterProvider
    {
        public InstanceProviderProvider(Func<IServiceProvider, object> factory)
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
            var provider = Providers.GetOrAdd(dispatcher, d =>
                (IFilter)Activator.CreateInstance(
                    typeof(InstanceProvider<>).MakeGenericType(d.LogicalReturnType)));
            return new [] { provider };
        }

        private static readonly ConcurrentDictionary<MemberDispatch, IFilter>
            Providers = new ConcurrentDictionary<MemberDispatch, IFilter>();
    }
}
