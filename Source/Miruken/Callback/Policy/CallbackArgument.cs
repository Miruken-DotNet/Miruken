namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

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
            var paramType = parameter.ParameterType;
            if (paramType.IsGenericParameter)
            {
                var contraints = paramType.GetGenericParameterConstraints();
                switch (contraints.Length)
                {
                    case 0:
                        paramType = typeof(object);
                        break;
                    case 1:
                        paramType = contraints[0];
                        break;
                    default:
                        return false;
                }
            }
            var restrict = attribute.Key as Type;
            if (restrict == null || paramType.Is(restrict) || restrict.Is(paramType))
            {
                if (_alias != null)
                    aliases.Add(_alias, paramType);
                return true;
            }
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {paramType.FullName}");
        }

        public override void Configure(ParameterInfo parameter,
            ref PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            var key       = policyMethodBindingInfo.Key;
            var restrict  = key as Type;
            var paramType = parameter.ParameterType;
            policyMethodBindingInfo.CallbackIndex = parameter.Position;
            if (paramType.IsGenericParameter)
            {
                var contraints = paramType.GetGenericParameterConstraints();
                paramType = contraints.Length == 1
                          ? contraints[0]
                          : typeof(object);
            }
            if (paramType != typeof(object) &&
                (restrict == null || paramType.Is(restrict)))
                policyMethodBindingInfo.Key = paramType;
        }

        public override object Resolve(object callback)
        {
            return callback;
        }
    }

    public class CallbackArgument<Cb> : ArgumentRule
    {
        public static readonly CallbackArgument<Cb>
             Instance = new CallbackArgument<Cb>();

        private CallbackArgument()
        {         
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            return paramType.Is<Cb>();
        }

        public override void Configure(ParameterInfo parameter,
            ref PolicyMethodBindingInfo policyMethodBindingInfo) { }

        public override object Resolve(object callback)
        {
            return callback;
        }
    }
}