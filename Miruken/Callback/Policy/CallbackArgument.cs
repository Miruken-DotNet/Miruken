namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public class CallbackArgument<Attrib> : ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public static readonly CallbackArgument<Attrib>
            Instance = new CallbackArgument<Attrib>();

        private CallbackArgument()
        {           
        }

        public override bool Matches(ParameterInfo parameter, Attrib attribute)
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
            ParameterInfo parameter, MethodDefinition<Attrib> method)
        {
            var paramType = parameter.ParameterType;
            if (paramType == typeof(object)) return;
            method.VarianceType = paramType;
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return callback;
        }
    }

    public class CallbackArgument<Attrib, Cb> : ArgumentRule<Attrib>
         where Attrib : DefinitionAttribute
    {
        public static readonly CallbackArgument<Attrib, Cb>
             Instance = new CallbackArgument<Attrib, Cb>();

        private CallbackArgument()
        {         
        }

        public override bool Matches(ParameterInfo parameter, Attrib attribute)
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