namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Reflection;

    public abstract class ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public abstract bool Matches(
           ParameterInfo parameter, Attrib attribute);

        public virtual void Configure(
            ParameterInfo parameter, MethodDefinition<Attrib> method)
        {         
        }

        public abstract object Resolve(object callback, IHandler composer);

        protected ICallbackFilter[] GetFilters(ParameterInfo parameter)
        {
            return (CallbackFilterAttribute[])parameter
                .GetCustomAttributes(typeof(CallbackFilterAttribute));
        }

        protected ICallbackFilter[] GetFilters(
            ParameterInfo parameter, Func<object, object> dependency)
        {
            return parameter
                .GetCustomAttributes(typeof(CallbackFilterAttribute))
                .Cast<CallbackFilterAttribute>()
                .Select(filter => new CallbackDependencyFilter(filter, dependency))
                .ToArray();
        }
    }
}