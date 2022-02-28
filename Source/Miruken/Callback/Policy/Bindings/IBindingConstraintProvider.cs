namespace Miruken.Callback.Policy.Bindings;

public interface IBindingConstraintProvider
{
    IBindingConstraint Constraint { get; }
}