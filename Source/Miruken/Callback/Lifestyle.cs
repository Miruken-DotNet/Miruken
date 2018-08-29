namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
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
        private object _lifestyle;
        private bool _initialized;

        protected LifestyleAttribute(Type lifestyleType)
        {
            if (lifestyleType == null)
                throw new ArgumentNullException(nameof(lifestyleType));

            if (!lifestyleType.IsGenericTypeDefinition ||
                lifestyleType.GetOpenTypeConformance(typeof(Lifestyle<>)) == null)
                throw new ArgumentException(
                    $"Type {lifestyleType} is not a Lifestyle functor");

            _lifestyle = lifestyleType;
        }

        public bool Required { get; } = true;

        IEnumerable<IFilter> IFilterProvider.GetFilters(
            MemberBinding binding, Type callbackType,
            Type logicalResultType, IHandler composer)
        {
            if (!_initialized)
            {
                object guard = this;
                LazyInitializer.EnsureInitialized(
                    ref _lifestyle, ref _initialized, ref guard,
                    () => Activator.CreateInstance(((Type)_lifestyle)
                            .MakeGenericType(logicalResultType)));
            }
            return new [] { (IFilter)_lifestyle };
        }

        void IValidateFilterProvider.Validate(MemberBinding binding)
        {
            if ((binding as PolicyMemberBinding)
                ?.Category.CallbackPolicy != Provides.Policy)
                throw new InvalidOperationException(
                    $"{_lifestyle} can only be applied to Providers");
        }
    }
}
