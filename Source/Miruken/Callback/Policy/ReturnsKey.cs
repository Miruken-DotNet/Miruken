namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

    public class ReturnsKey : ReturnRule
    {
        public static readonly ReturnsKey Instance = new ReturnsKey();

        private ReturnsKey()
        {        
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            if (IsLogicalVoid(returnType)) return false;
            if (returnType.IsArray)
                returnType = returnType.GetElementType();
            var restrict = attribute.Key as Type;
            if (restrict == null || returnType.Is(restrict) || restrict.Is(returnType))
                return true;
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {returnType.FullName}");
        }

        public override void Configure(
            ref PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            var key      = policyMethodBindingInfo.Key;
            var restrict = key as Type;
            if (key != null && restrict == null) return;
            var dispatch   = policyMethodBindingInfo.Dispatch;
            var returnType = dispatch.LogicalReturnType;
            if (!policyMethodBindingInfo.Definition.Strict)
            {
                if (returnType.IsArray)
                    returnType = returnType.GetElementType();
                if (returnType.IsSimpleType())
                {
                    var method     = dispatch.Method;
                    var methodName = method.Name;
                    if (method.IsSpecialName)
                    {
                        var _ = methodName.IndexOf('_');
                        if (_ >= 0) methodName = methodName.Substring(_ + 1);
                    }
                    policyMethodBindingInfo.Key = new StringKey(
                        methodName, StringComparison.OrdinalIgnoreCase);
                    return;
                }
            }
            if (returnType != typeof(object) &&
                (restrict == null || returnType.Is(restrict)))
                policyMethodBindingInfo.Key = returnType;
        }
    }
}
