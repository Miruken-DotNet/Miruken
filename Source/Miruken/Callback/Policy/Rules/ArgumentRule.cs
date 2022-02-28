namespace Miruken.Callback.Policy.Rules;

using System;
using System.Linq;
using System.Reflection;
using Bindings;
using Infrastructure;

public abstract class ArgumentRule
{
    public ArgumentAlias Alias(string alias)
    {
        if (string.IsNullOrEmpty(alias))
            throw new ArgumentException(
                @"Argument alias cannot be empty", nameof(alias));
        return new ArgumentAlias(alias, this);
    }

    public TypedArgument OfType<TArg>(params string[] aliases) => OfType(typeof(TArg), aliases);

    public TypedArgument OfType(Type argType, params string[] aliases) => new(this, argType, aliases);

    public abstract bool Matches(ParameterInfo parameter, RuleContext context);

    public virtual void Configure(
        ParameterInfo           parameter,
        PolicyMemberBindingInfo policyMemberBindingInfo) { }

    public abstract object Resolve(object callback);
}

public abstract class ArgumentRuleDecorator : ArgumentRule
{
    protected ArgumentRuleDecorator(ArgumentRule argument)
    {
        Argument = argument 
                   ?? throw new ArgumentNullException(nameof(argument));
    }

    public ArgumentRule Argument { get; }

    public override bool Matches(ParameterInfo parameter, RuleContext context) =>
        Argument.Matches(parameter, context);

    public override void Configure(ParameterInfo parameter,
        PolicyMemberBindingInfo policyMemberBindingInfo) =>
        Argument.Configure(parameter, policyMemberBindingInfo);

    public override object Resolve(object callback) => Argument.Resolve(callback);
}

public class ArgumentAlias : ArgumentRuleDecorator
{
    private readonly string _alias;

    public ArgumentAlias(string alias, ArgumentRule argument)
        : base(argument)
    {
        _alias = alias;
    }

    public override bool Matches(ParameterInfo parameter, RuleContext context) =>
        base.Matches(parameter, context) &&
        context.AddAlias(_alias, parameter.ParameterType);
}

public class TypedArgument : ArgumentRuleDecorator
{
    private readonly Type _type;
    private readonly string[] _aliases;

    public TypedArgument(ArgumentRule argument, Type type,
        params string[] aliases)
        : base(argument)
    {
        _type    = type ?? throw new ArgumentNullException(nameof(type));
        _aliases = aliases;
    }

    public override bool Matches(
        ParameterInfo parameter, RuleContext context)
    {
        var paramType = parameter.ParameterType;
        if (paramType.Is(_type))
        {
            if (!base.Matches(parameter, context)) return false;
            if (_aliases is {Length: > 0})
            {
                context.AddError(
                    $"{_type.FullName} is not a generic definition and cannot bind aliases");
                return false;
            }
            return true;
        }
        var openGeneric = paramType.GetOpenTypeConformance(_type);
        if (openGeneric == null) return false;
        if (_aliases == null || _aliases.Length == 0)
            return base.Matches(parameter, context);
        var genericArgs = openGeneric.GetGenericArguments();
        if (_aliases.Length > genericArgs.Length)
        {
            context.AddError(
                $"{_type.FullName} has {genericArgs.Length} generic args, but {_aliases.Length} aliases provided");
            return false;
        }
        return base.Matches(parameter, context) && 
               !_aliases.Where((alias, i) => !string.IsNullOrEmpty(alias) &&
                                             !context.AddAlias(alias, genericArgs[i])).Any();
    }
}