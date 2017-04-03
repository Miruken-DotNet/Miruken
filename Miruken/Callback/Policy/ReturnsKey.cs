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

        public override bool Matches(MethodDefinition<Attrib> method)
        {
            if (method.IsVoid) return false;
            var returnType = method.ReturnType;
            if (returnType.IsArray)
                returnType = returnType.GetElementType();
            Type varianceType;
            var restrict = method.Attribute.Key as Type;
            if (restrict == null || restrict.IsAssignableFrom(returnType))
                varianceType = returnType;
            else if (returnType.IsAssignableFrom(restrict))
                varianceType = restrict;
            else
                throw new InvalidOperationException(
                    $"Key {restrict.FullName} is not related to {returnType.FullName}");
            method.VarianceType = varianceType;
            method.AddFilters(new CovariantFilter<Cb>(varianceType, _key));
            return true;
        }
    }
}
