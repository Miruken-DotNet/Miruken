namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    [AttributeUsage(
        AttributeTargets.Class    | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Parameter |
        AttributeTargets.Constructor,
        Inherited = false)]
    public class ConstraintAttribute : Attribute, IFilterProvider
    {
        public ConstraintAttribute(object key, object value)
            : this(new MetadataKey(key, value))
        {
        }

        public ConstraintAttribute(IDictionary<object, object> metadata)
            : this(new Metadata(metadata))
        {
        }

        public ConstraintAttribute(IBindingConstraint constraint)
        {
            Constraint = constraint 
                      ?? throw new ArgumentNullException(nameof(constraint));
        }

        public bool Required { get; } = true;

        public IBindingConstraint Constraint { get; }

        IEnumerable<IFilter> IFilterProvider.GetFilters(
            MemberBinding binding, Type callbackType,
            Type logicalResultType, IHandler composer)
        {
            return new[]
            {
                Constraints.GetOrAdd(logicalResultType, r =>
                    (IFilter) Activator.CreateInstance(
                        typeof(ConstraintFilter<>).MakeGenericType(r)))
            };
        }

        private static readonly ConcurrentDictionary<Type, IFilter>
            Constraints = new ConcurrentDictionary<Type, IFilter>();
    }
}
