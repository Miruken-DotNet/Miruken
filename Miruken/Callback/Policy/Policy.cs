namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public abstract class Policy<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly List<MethodRule<Attrib>> _methods;

        protected Policy()
        {
            _methods = new List<MethodRule<Attrib>>();
        }

        public virtual Type CallbackType => null;

        public void AddMethod(MethodRule<Attrib> method)
        {
            _methods.Add(method);
        }

        public MethodDefinition<Attrib> Match(MethodInfo method, Attrib attribute)
        {
            return Match(method, attribute, _methods);
        }

        protected abstract MethodDefinition<Attrib> Match(
            MethodInfo method, Attrib attribute, 
            IEnumerable<MethodRule<Attrib>> rules);
    }
}