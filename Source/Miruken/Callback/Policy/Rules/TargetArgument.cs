namespace Miruken.Callback.Policy.Rules;

using System;
using System.Reflection;
using Bindings;
using Infrastructure;

public class TargetArgument<TCb> : ArgumentRule
{
    private readonly Func<TCb, object> _target;

    public TargetArgument(Func<TCb, object> target)
    {
        _target = target 
                  ?? throw new ArgumentNullException(nameof(target));
    }

    public override bool Matches(
        ParameterInfo parameter, RuleContext context)
    {
        var paramType = parameter.ParameterType;
        if (paramType.Is<TCb>()) return false;
        if (paramType.IsGenericParameter)
        {
            var constraints = paramType.GetGenericParameterConstraints();
            switch (constraints.Length)
            {
                case 0:
                    paramType = typeof(object);
                    break;
                case 1:
                    paramType = constraints[0];
                    break;
                default:
                    return false;
            }
        }

        if (context.Category.InKey is not Type restrict ||
            paramType.Is(restrict) || restrict.Is(paramType))
            return true;

        context.AddError(
            $"Key {restrict.FullName} is not related to {paramType.FullName}");
        return false;
    }

    public override void Configure(ParameterInfo parameter,
        PolicyMemberBindingInfo policyMemberBindingInfo)
    {
        var key       = policyMemberBindingInfo.InKey;
        var restrict  = key as Type;
        policyMemberBindingInfo.CallbackIndex = parameter.Position;
        if (key != null && restrict == null) return;
        var paramType = parameter.ParameterType;
        if (paramType.IsGenericParameter)
        {
            var constraints = paramType.GetGenericParameterConstraints();
            paramType = constraints.Length == 1
                ? constraints[0]
                : typeof(object);
        }
        if (paramType != typeof(object) &&
            (restrict == null || paramType.Is(restrict)))
            policyMemberBindingInfo.InKey = paramType;
    }

    public override object Resolve(object callback)
    {
        return _target((TCb) callback);
    }
}