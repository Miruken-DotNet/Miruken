namespace Miruken.Callback.Policy.Bindings;

public interface IBindingConstraint
{
    void Require(BindingMetadata metadata);

    bool Matches(BindingMetadata metadata);
}