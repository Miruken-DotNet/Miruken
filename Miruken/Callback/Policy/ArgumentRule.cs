namespace Miruken.Callback.Policy
{
    using System;
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

        public TypedArgument OfType(Type argType)
        {
            return new TypedArgument(this, argType);
        }

        public abstract bool Matches(
           ParameterInfo parameter, DefinitionAttribute attribute);

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
            ParameterInfo parameter, DefinitionAttribute attribute)
        {
            return Argument.Matches(parameter, attribute);
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

        public TypedArgument(ArgumentRule argument, Type type)
            : base(argument)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _type = type;
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute)
        {
            return parameter.ParameterType.IsClassOf(_type);
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