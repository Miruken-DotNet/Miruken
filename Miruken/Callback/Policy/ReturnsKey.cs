namespace Miruken.Callback.Policy
{
    using System;

    public class ReturnsKey<Attrib, Cb> : ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly Func<Cb, object> _key;

        public ReturnsKey(Func<Cb, object> key)
        {
            _key = key;
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
            var restrict = method.Attribute.Key as Type;
            var varianceType = restrict == null 
                            || restrict.IsAssignableFrom(returnType)
                             ? returnType : restrict;
            method.VarianceType = varianceType;
            method.AddFilters(new CovariantFilter<Cb>(varianceType, _key));
        }
    }
}
