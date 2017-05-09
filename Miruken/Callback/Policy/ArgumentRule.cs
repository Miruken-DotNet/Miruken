namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

    public abstract class ArgumentRule
    {
        public OptionalArgument Optional =>
            this as OptionalArgument ?? new OptionalArgument(this);

        public TypedArgument OfType<TArg>()
        {
            return OfType(typeof(TArg));
        }

        public TypedArgument OfType(Type argType, params string[] aliases)
        {
            return new TypedArgument(this, argType, aliases);
        }

        public abstract bool Matches(
           ParameterInfo parameter, DefinitionAttribute attribute,
           IDictionary<string, Type> aliases);

        public virtual void Configure(
            ParameterInfo parameter, PolicyMethodBinding binding) { }

        public abstract object Resolve(object callback, IHandler composer);
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
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            return Argument.Matches(parameter, attribute, aliases);
        }

        public override void Configure(
            ParameterInfo parameter, PolicyMethodBinding binding)
        {
            Argument.Configure(parameter, binding);
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return Argument.Resolve(callback, composer);
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
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            if (_type.IsAssignableFrom(paramType))
            {
                if (_aliases != null && _aliases.Length > 0)
                    throw new InvalidOperationException(
                        $"{_type.FullName} is not a generic definition and cannot bind aliases");
                return base.Matches(parameter, attribute, aliases);
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
            return base.Matches(parameter, attribute, aliases);
        }
    }

    public interface IOptional { }

    public class OptionalArgument : ArgumentRuleDecorator, IOptional
    {
        public OptionalArgument(ArgumentRule argument)
            : base(argument)
        {
        }
    }
}