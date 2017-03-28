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

        public override bool Matches(Attrib definition, ParameterInfo parameter)
        {
            Type varianceType;
            var restrict  = definition.Key as Type;
            var paramType = parameter.ParameterType;
            if (restrict == null || restrict.IsAssignableFrom(paramType))
                varianceType = paramType;                
            else if (paramType.IsAssignableFrom(restrict))
                varianceType = restrict;
            else
                throw new InvalidOperationException(
                    $"Key {restrict.FullName} is not related to {paramType.FullName}");
            definition.VarianceType = varianceType;     
            definition.AddFilters(CreateTypeFilter(definition, varianceType));
            return true;
        }

        public override object Resolve(Attrib definition, object callback, IHandler composer)
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

        public override bool Matches(Attrib definition, ParameterInfo parameter)
        {
            var paramType = parameter.ParameterType;
            if (typeof(Cb).IsAssignableFrom(paramType))
            {
                definition.VarianceType = typeof(Cb);
                definition.AddFilters(CreateTypeFilter(definition, typeof(Cb)));
                return true;
            }
            return false;
        }

        public override object Resolve(Attrib definition, object callback, IHandler composer)
        {
            return callback;
        }
    }
}