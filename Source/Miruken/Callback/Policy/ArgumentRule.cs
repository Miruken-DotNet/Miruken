namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

    public abstract class ArgumentRule
    {
        public AliasedArgument Alias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentException(
                    @"Argument alias cannot be empty", nameof(alias));
            return new AliasedArgument(alias, this);
        }

        public TypedArgument OfType<TArg>(params string[] aliases)
        {
            return OfType(typeof(TArg), aliases);
        }

        public TypedArgument OfType(Type argType, params string[] aliases)
        {
            return new TypedArgument(this, argType, aliases);
        }

        public abstract bool Matches(
           ParameterInfo parameter, CategoryAttribute category,
           IDictionary<string, Type> aliases);

        public virtual void Configure(ParameterInfo parameter,
            PolicyMethodBindingInfo policyMethodBindingInfo) { }

        public abstract object Resolve(object callback);
    }

    public abstract class ArgumentRuleDecorator : ArgumentRule
    {
        protected ArgumentRuleDecorator(ArgumentRule argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));
            Argument = argument;
        }

        public ArgumentRule Argument { get; }

        public override bool Matches(
            ParameterInfo parameter, CategoryAttribute category,
            IDictionary<string, Type> aliases)
        {
            return Argument.Matches(parameter, category, aliases);
        }

        public override void Configure(ParameterInfo parameter,
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            Argument.Configure(parameter, policyMethodBindingInfo);
        }

        public override object Resolve(object callback)
        {
            return Argument.Resolve(callback);
        }
    }

    public class AliasedArgument : ArgumentRuleDecorator
    {
        private readonly string _alias;

        public AliasedArgument(string alias, ArgumentRule argument)
            : base(argument)
        {
            _alias = alias;
        }

        public override bool Matches(
            ParameterInfo parameter, CategoryAttribute category,
            IDictionary<string, Type> aliases)
        {
            Type alias;
            return aliases.TryGetValue(_alias, out alias) &&
                parameter.ParameterType.Is(alias);
        }
    }

    public class TypedArgument : ArgumentRuleDecorator
    {
        private readonly Type _type;
        private readonly string[] _aliases;

        public TypedArgument(ArgumentRule argument, Type type,
                             params string[] aliases)
            : base(argument)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _type    = type;
            _aliases = aliases;
        }

        public override bool Matches(
            ParameterInfo parameter, CategoryAttribute category,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            if (paramType.Is(_type))
            {
                if (_aliases != null && _aliases.Length > 0)
                    throw new InvalidOperationException(
                        $"{_type.FullName} is not a generic definition and cannot bind aliases");
                return base.Matches(parameter, category, aliases);
            }
            var openGeneric = paramType.GetOpenTypeConformance(_type);
            if (openGeneric == null) return false;
            if (_aliases == null || _aliases.Length == 0) return true;
            var genericArgs = openGeneric.GetGenericArguments();
            if (_aliases.Length > genericArgs.Length)
                throw new InvalidOperationException(
                    $"{_type.FullName} has {genericArgs.Length} generic args, but {_aliases.Length} requested");
            for (var i = 0; i < _aliases.Length; ++i)
            {
                var alias = _aliases[i];
                if (!string.IsNullOrEmpty(alias))
                    aliases.Add(_aliases[i], genericArgs[i]);
            }
            return base.Matches(parameter, category, aliases);
        }
    }
}