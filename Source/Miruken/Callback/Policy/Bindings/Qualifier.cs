namespace Miruken.Callback.Policy.Bindings
{
    using System;

    public class Qualifier : IBindingConstraint
    {
        private readonly Type _attributeClass;

        private Qualifier(Type attributeClass)
        {
            _attributeClass = attributeClass;
        }

        public void Require(BindingMetadata metadata)
        {
            metadata.Set(_attributeClass, null);
        }

        public bool Matches(BindingMetadata metadata)
        {
            return metadata.IsEmpty || metadata.Has(_attributeClass);
        }

        public static Qualifier Of(Type attributeClass)
        {
            if (attributeClass == null)
                throw new ArgumentNullException(nameof(attributeClass));
            if (!typeof(Attribute).IsAssignableFrom(attributeClass))
                throw new ArgumentException($"{attributeClass.FullName} is not an Attribute class");
            return new Qualifier(attributeClass);
        }

        public static Qualifier Of<T>() where T : Attribute
        {
            return new Qualifier(typeof(T));
        }
    }
}
