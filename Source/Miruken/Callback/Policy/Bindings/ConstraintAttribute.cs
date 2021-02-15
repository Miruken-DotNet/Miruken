namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Parameter |
        AttributeTargets.Constructor,
        Inherited = false)]
    public class ConstraintAttribute : Attribute,
        IFilterProvider, IBindingConstraintProvider
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

        protected ConstraintAttribute()
        {
        }

        public bool Required { get; } = true;

        public IBindingConstraint Constraint { get; protected set; }

        public bool? AppliesTo(object callback, Type callbackType)
        {
            return callback is IBindingScope;
        }

        public IEnumerable<IFilter> GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            object callback, Type callbackType, IHandler composer)
        {
            return new[]
            {
                Constraints.GetOrAdd(dispatcher.LogicalReturnType, r =>
                    (IFilter)Activator.CreateInstance(
                        typeof(ConstraintFilter<>).MakeGenericType(r)))
            };
        }

        private static readonly ConcurrentDictionary<Type, IFilter>
            Constraints = new();
    }
}
