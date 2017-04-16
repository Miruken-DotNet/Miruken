namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public class CallbackArgument : ArgumentRule
    {
        public static readonly CallbackArgument
            Instance = new CallbackArgument();

        private CallbackArgument()
        {           
        }

        public override bool Matches(ParameterInfo parameter, DefinitionAttribute attribute)
        {
            var restrict  = attribute.Key as Type;
            var paramType = parameter.ParameterType;
            if (restrict == null || restrict.IsAssignableFrom(paramType)
                || paramType.IsAssignableFrom(restrict))
                return true;
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {paramType.FullName}");
        }

        public override void Configure(
            ParameterInfo parameter, MethodBinding binding)
        {
            var paramType = parameter.ParameterType;
            if (paramType == typeof(object)) return;
            binding.VarianceType = paramType;
            binding.AddFilters(GetFilters(parameter));
        }

        public override object Resolve(object callback, IHandler composer)
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

        public override bool Matches(ParameterInfo parameter, DefinitionAttribute attribute)
        {
            var paramType = parameter.ParameterType;
            return typeof(Cb).IsAssignableFrom(paramType);
        }

        public override void Configure(
            ParameterInfo parameter, MethodBinding binding)
        {
            binding.AddFilters(GetFilters(parameter));
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return callback;
        }
    }
}