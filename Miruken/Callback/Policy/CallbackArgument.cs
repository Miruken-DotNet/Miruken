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

        public override bool Matches(
            MethodDefinition<Attrib> method, ParameterInfo parameter)
        {
            Type varianceType;
            var restrict  = method.Attribute.Key as Type;
            var paramType = parameter.ParameterType;
            if (restrict == null || restrict.IsAssignableFrom(paramType))
                varianceType = paramType;                
            else if (paramType.IsAssignableFrom(restrict))
                varianceType = restrict;
            else
                throw new InvalidOperationException(
                    $"Key {restrict.FullName} is not related to {paramType.FullName}");
            method.VarianceType = varianceType;     
            method.AddFilters(new ContravariantFilter(
                varianceType, method.Attribute.Invariant));
            return true;
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

        public override bool Matches(
            MethodDefinition<Attrib> method, ParameterInfo parameter)
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