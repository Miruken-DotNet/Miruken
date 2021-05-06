namespace Miruken.Callback
{
    using System;
    using Policy.Bindings;

    public class Named : IBindingConstraint
    {
        public Named(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be empty.");

            Name = name;
        }

        public string Name { get; }

        public void Require(BindingMetadata metadata)
        {
            metadata.Name = Name;
        }

        public bool Matches(BindingMetadata metadata)
        {
            return metadata.Name == null || metadata.Name == Name;
        }
    }

    public class NamedAttribute : ConstraintAttribute
    {
        public NamedAttribute(string name)
            : base(new Named(name))
        {       
        }
    }
}
