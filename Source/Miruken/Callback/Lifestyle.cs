namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure;
    using Policy;

    public abstract class Lifestyle<T> : IFilter<Inquiry, T>
    {
        public int? Order { get; set; }

        public Task<T> Next(Inquiry callback,
            MemberBinding member, IHandler composer,
            Next<T> next, IFilterProvider provider)
        {
            return GetInstance(member, next, composer, out var instance)
                 ? Task.FromResult(instance)
                 : null;
        }

        protected abstract bool GetInstance(
            MemberBinding member,  Next<T> next,
            IHandler composer, out T instance);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property |
                    AttributeTargets.Constructor, Inherited = false)]
    public abstract class LifestyleAttribute
        : Attribute, IFilterProvider, IValidateFilterProvider
    {
        protected LifestyleAttribute(Type lifestyleType)
        {
            if (lifestyleType == null)
                throw new ArgumentNullException(nameof(lifestyleType));

            if (!lifestyleType.IsGenericTypeDefinition ||
                lifestyleType.GetOpenTypeConformance(typeof(Lifestyle<>)) == null)
                throw new ArgumentException(
                    $"Type {lifestyleType} is not a Lifestyle functor");

            LifestyleType = lifestyleType;
        }

        public bool Required { get; } = true;

        public Type LifestyleType { get; }

        IEnumerable<IFilter> IFilterProvider.GetFilters(
            MemberBinding binding, Type callbackType,
            Type logicalResultType, IHandler composer)
        {
            return new[]
            {
                Lifestyles.GetOrAdd((binding, logicalResultType), b =>
                    (IFilter) Activator.CreateInstance(
                        LifestyleType.MakeGenericType(b.Item2)))
            };
        }

        void IValidateFilterProvider.Validate(MemberBinding binding)
        {
            if ((binding as PolicyMemberBinding)
                ?.Category.CallbackPolicy != Provides.Policy)
                throw new InvalidOperationException(
                    $"{GetType().FullName} can only be applied to Providers");

            if (binding.Dispatcher.Attributes.OfType<LifestyleAttribute>()
                .Any(lifestyle => !ReferenceEquals(lifestyle, this)))
                throw new InvalidOperationException(
                    "Only one Lifestyle attribute is allowed");
        }

        private static readonly ConcurrentDictionary<(MemberBinding, Type), IFilter>
            Lifestyles = new ConcurrentDictionary<(MemberBinding, Type), IFilter>();
    }
}
