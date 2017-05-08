namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class CallbackArgument : ArgumentRule
    {
        private string _alias;

        public static readonly CallbackArgument
            Instance = new CallbackArgument();

        private CallbackArgument(string alias = null)
        {
            _alias = alias;
        }

        public CallbackArgument this[string alias]
        {
            get
            {
                if (string.IsNullOrEmpty(alias))
                    throw new ArgumentException("Alias cannot be empty", nameof(alias));
                if (this == Instance)
                    return new CallbackArgument(alias);
                _alias = alias;
                return this;
            }   
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var restrict  = attribute.Key as Type;
            var paramType = parameter.ParameterType;
            if (restrict == null || restrict.IsAssignableFrom(paramType)
                || paramType.IsAssignableFrom(restrict))
            {
                if (_alias != null)
                    aliases.Add(_alias, paramType);
                return true;
            }
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {paramType.FullName}");
        }

        public override void Configure(
            ParameterInfo parameter, PolicyMethodBinding binding)
        {
            base.Configure(parameter, binding);
            var paramType = parameter.ParameterType;
            if (paramType == typeof(object)) return;
            binding.VarianceType = paramType;
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return callback;
        }
    }

    public class CallbackArgument<Cb> : ArgumentRule
    {
        private readonly Func<Cb, object> _target;

        public static readonly CallbackArgument<Cb>
             Instance = new CallbackArgument<Cb>();

        private CallbackArgument()
        {         
        }

        public CallbackArgument(Func<Cb, object> target)
        {
            _target = target;
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            return typeof(Cb).IsAssignableFrom(paramType);
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return callback;
        }
    }
}