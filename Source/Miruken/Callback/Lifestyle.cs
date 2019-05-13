﻿namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure;
    using Policy;
    using Policy.Bindings;

    public interface ILifestyle : IFilter
    {
        LifestyleAttribute Attribute { get; set; }
    }

    public abstract class Lifestyle<T> : IFilter<Inquiry, T>, ILifestyle
    {
        public int? Order { get; set; } = int.MaxValue - 100;

        public LifestyleAttribute Attribute { get; set; }

        public Task<T> Next(Inquiry callback,
            object rawCallback, MemberBinding member,
            IHandler composer, Next<T> next,
            IFilterProvider provider)
        {
            var parent = callback.Parent;
            return parent == null || IsCompatibleWithParent(parent)
                 ? GetInstance(callback, member, next, composer)
                 : null;
        }

        protected abstract bool IsCompatibleWithParent(Inquiry parent);

        protected abstract Task<T> GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer);
    }

    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property |
        AttributeTargets.Constructor,
        Inherited = false)]
    public abstract class LifestyleAttribute
        : Attribute, IFilterProvider
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

        IEnumerable<IFilter> IFilterProvider.GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            Type callbackType, IHandler composer)
        {
            var lifestyle = Lifestyles.GetOrAdd(dispatcher, d =>
            {
                var l = (ILifestyle) Activator.CreateInstance(
                    LifestyleType.MakeGenericType(d.LogicalReturnType));
                l.Attribute = this;
                return l;
            });

            var filters = new [] { lifestyle };
            return !(this is IBindingConstraintProvider provider) ? filters
                : filters.Concat(new ConstraintAttribute(provider.Constraint)
                    .GetFilters(binding, dispatcher, callbackType, composer));
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || GetType() == obj?.GetType();
        }

        public override int GetHashCode()
        {
            return LifestyleType.GetHashCode();
        }

        private static readonly ConcurrentDictionary<MemberDispatch, IFilter>
            Lifestyles = new ConcurrentDictionary<MemberDispatch, IFilter>();
    }
}
