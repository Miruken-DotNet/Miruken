namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public class ReturnsKey : ReturnRule
    {
        public static readonly ReturnsKey Instance = new ReturnsKey();

        private ReturnsKey()
        {        
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute)
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

        public override void Configure(PolicyMethodBinding binding)
        {
            var returnType = binding.Dispatcher.ReturnType;
            if (returnType.IsArray)
                returnType = returnType.GetElementType();
            if (returnType == typeof(object)) return;
            binding.VarianceType = returnType;
        }
    }
}
