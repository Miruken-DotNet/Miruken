namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public abstract class ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public abstract bool Matches(Attrib definition, ParameterInfo parameter);

        public abstract object Resolve(Attrib definition, object callback, IHandler composer);

        protected CallbackTypeFilter CreateTypeFilter(Attrib definition, Type type,
            Func<object, object> extract = null)
        {
            return new CallbackTypeFilter(type, extract) { Invariant = definition.Invariant };
        }
    }
}