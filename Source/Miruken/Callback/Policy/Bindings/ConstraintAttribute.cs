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
            : this(new MetadataKeyConstraint(key, value))
        {
        }

        public ConstraintAttribute(IDictionary<object, object> metadata)
            : this(new MetadataConstraint(metadata))
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
            MemberBinding binding, MemberDispatch dispatcher,
            Type callbackType, IHandler composer)
        {
            return new[]
            {
                Constraints.GetOrAdd(dispatcher.LogicalReturnType, r =>
                    (IFilter) Activator.CreateInstance(
                        typeof(ConstraintFilter<>).MakeGenericType(r)))
            };
        }

        private static readonly ConcurrentDictionary<Type, IFilter>
            Constraints = new ConcurrentDictionary<Type, IFilter>();
    }
}
