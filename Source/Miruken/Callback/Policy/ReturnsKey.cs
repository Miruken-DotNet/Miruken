namespace Miruken.Callback.Policy
{
    using System;
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
            RuleContext context)
        {
            if (IsLogicalVoid(returnType)) return false;
            if (returnType.IsArray)
                returnType = returnType.GetElementType();
            var restrict = context.Category.OutKey as Type;
            if (restrict == null || returnType.Is(restrict) || restrict.Is(returnType))
                return true;
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {returnType?.FullName}");
        }

        public override void Configure(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            var key      = policyMethodBindingInfo.OutKey;
            var restrict = key as Type;
            if (key != null && restrict == null) return;
            var dispatch   = policyMethodBindingInfo.Dispatch;
            var returnType = dispatch.LogicalReturnType;
            if (!policyMethodBindingInfo.Category.Strict)
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
                    policyMethodBindingInfo.OutKey = new StringKey(
                        methodName, StringComparison.OrdinalIgnoreCase);
                    return;
                }
            }
            if (returnType != typeof(object) &&
                (restrict == null || returnType.Is(restrict)))
                policyMethodBindingInfo.OutKey = returnType;
        }
    }
}
