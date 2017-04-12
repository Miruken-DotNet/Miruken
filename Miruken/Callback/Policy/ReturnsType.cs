namespace Miruken.Callback.Policy
{
    using System;

    public class ReturnsType<Attrib> : ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly Type _type;

        public ReturnsType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _type = type;
        }

        public override bool Matches(Type returnType, Attrib attribute)
        {
            return _type.IsAssignableFrom(returnType);
        }
    }

    public class ReturnsType<T, Attrib> : ReturnsType<Attrib>
        where Attrib : DefinitionAttribute
    {
        public static readonly ReturnRule<Attrib>
            Instance = new ReturnsType<T, Attrib>();

        public new static readonly ReturnRule<Attrib>
            OrVoid = Instance.OrVoid;

        private ReturnsType() : base(typeof(T))
        {         
        }
    }
}
