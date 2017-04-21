namespace Miruken.Callback.Policy
{
    using System.Reflection;

    public abstract class ArgumentRule
    {
        public OptionalArgument Optional => this as OptionalArgument ??
                                            new OptionalArgument(this);

        public abstract bool Matches(
           ParameterInfo parameter, DefinitionAttribute attribute);

        public virtual void Configure(
            ParameterInfo parameter, MethodBinding binding)
        {         
        }

        public abstract object Resolve(object callback, IHandler composer);
    }
}