namespace Miruken.Callback.Policy
{
    using System;

    public class ReturnsType : ReturnRule
    {
        private readonly Type _type;

        public ReturnsType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _type = type;
        }

        public override bool Matches(Type returnType, DefinitionAttribute attribute)
        {
            return _type.IsAssignableFrom(returnType);
        }
    }

    public class ReturnsType<T> : ReturnsType
    {
        public static readonly ReturnRule Instance = new ReturnsType<T>();

        public new static readonly ReturnRule OrVoid = Instance.OrVoid;

        private ReturnsType() : base(typeof(T))
        {         
        }
    }
}
