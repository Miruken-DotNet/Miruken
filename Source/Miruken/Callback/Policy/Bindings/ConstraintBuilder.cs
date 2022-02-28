namespace Miruken.Callback.Policy.Bindings;

using System;

public class ConstraintBuilder
{
    private readonly BindingMetadata _metadata;

    public ConstraintBuilder(BindingMetadata metadata = null)
    {
        _metadata = metadata ?? new BindingMetadata();
    }

    public ConstraintBuilder(IBindingScope scope) : this(scope.Metadata)
    {
    }

    public ConstraintBuilder Named(string name)
    {
        return Require(new Named(name));
    }

    public ConstraintBuilder Require(object key, object value)
    {
        _metadata.Set(key, value);
        return this;
    }

    public ConstraintBuilder Require(IBindingConstraint constraint)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));
        constraint.Require(_metadata);
        return this;
    }

    public ConstraintBuilder Require(ConstraintAttribute constraint)
    {
        return Require(constraint.Constraint);
    }

    public ConstraintBuilder Require(BindingMetadata metadata)
    {
        if (metadata.Name != null)
            _metadata.Name = metadata.Name;
        metadata.MergeInto(_metadata);
        return this;
    }

    public BindingMetadata Build()
    {
        return _metadata;
    }

    public static BindingMetadata BuildConstraints(
        Action<ConstraintBuilder> constraints,
        BindingMetadata metadata = null)
    {
        if (constraints == null) return null;
        var builder = new ConstraintBuilder(metadata);
        constraints(builder);
        return builder.Build();
    }

    public static BindingMetadata BuildConstraints(
        IBindingScope bindingScope,
        Action<ConstraintBuilder> constraints)
    {
        var metadata = bindingScope.Metadata;
        return metadata == null ? null 
            : BuildConstraints(constraints, metadata);
    }
}