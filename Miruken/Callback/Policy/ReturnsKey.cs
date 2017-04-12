namespace Miruken.Callback.Policy
{
    using System;

    public class ReturnsKey<Attrib> : ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public static readonly ReturnsKey<Attrib> Instance = new ReturnsKey<Attrib>();

        private ReturnsKey()
        {        
        }

        public override bool Matches(Type returnType, Attrib attribute)
        {
            if (returnType == typeof(void)) return false;
            if (returnType.IsArray)
                returnType = returnType.GetElementType();
            var restrict = attribute.Key as Type;
            if (restrict == null || restrict.IsAssignableFrom(returnType)
                || returnType.IsAssignableFrom(restrict))
                return true;
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {returnType.FullName}");
        }

        public override void Configure(MethodDefinition<Attrib> method)
        {
            var returnType = method.ReturnType;
            if (returnType.IsArray)
                returnType = returnType.GetElementType();
            if (returnType == typeof(object)) return;
            method.VarianceType = returnType;
        }
    }
}
