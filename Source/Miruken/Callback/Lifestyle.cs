namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure;
    using Policy;
    using Policy.Bindings;

    public abstract class Lifestyle<T> : IFilter<Inquiry, T>
    {
        public int? Order { get; set; } = int.MaxValue - 100;

        public Task<T> Next(Inquiry callback,
            object rawCallback, MemberBinding member,
            IHandler composer, Next<T> next,
            IFilterProvider provider)
        {
            var parent    = callback.Parent;
            var attribute = provider as LifestyleAttribute;
            if (parent == null || IsCompatibleWithParent(parent, attribute))
            {
                try
                {
                    return GetInstance(callback, member, next, composer, attribute);
                }
                catch (Exception exception)
                {
                    return Task.FromException<T>(exception);
                }
            }
            return null;
        }

        protected abstract bool IsCompatibleWithParent(
            Inquiry parent, LifestyleAttribute attribute);

        protected abstract Task<T> GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer,
            LifestyleAttribute attribute);
    }

    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property |
        AttributeTargets.Constructor,
        Inherited = false)]
    public abstract class LifestyleAttribute : Attribute, IFilterProvider
    {
        protected LifestyleAttribute(Type lifestyleType)
        {
            if (lifestyleType == null)
                throw new ArgumentNullException(nameof(lifestyleType));

            if (!lifestyleType.IsGenericTypeDefinition ||
                lifestyleType.GetOpenTypeConformance(typeof(Lifestyle<>)) == null)
            {
                throw new ArgumentException(
                    $"Type {lifestyleType} is not a Lifestyle functor");
            }
    
            LifestyleType = lifestyleType;
        }

        public bool Required { get; } = true;

        public Type LifestyleType { get; }

        public bool? AppliesTo(object callback, Type callbackType)
        {
            return callback is Inquiry;
        }

        IEnumerable<IFilter> IFilterProvider.GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            object callback, Type callbackType, IHandler composer)
        {
            if (!(callback is Inquiry inquiry))
                return Enumerable.Empty<IFilter>();

            var logicalType  = dispatcher.LogicalReturnType;
            var key          = logicalType == typeof(object) ? inquiry.Key : null;
            var lifestyleKey = Tuple.Create(dispatcher, key);
            var lifestyle    = Lifestyles.GetOrAdd(lifestyleKey, k =>
                (IFilter)Activator.CreateInstance(LifestyleType.MakeGenericType(logicalType))
            );

            var filters = new [] { lifestyle };
            return !(this is IBindingConstraintProvider provider) ? filters
                : filters.Concat(new ConstraintAttribute(provider.Constraint)
                    .GetFilters(binding, dispatcher, callback, callbackType, composer));
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || GetType() == obj?.GetType();
        }

        public override int GetHashCode()
        {
            return LifestyleType.GetHashCode();
        }

        private static readonly ConcurrentDictionary<Tuple<MemberDispatch, object>, IFilter>
            Lifestyles = new ConcurrentDictionary<Tuple<MemberDispatch, object>, IFilter>();
    }
}
