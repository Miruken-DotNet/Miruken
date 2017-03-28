namespace Miruken.Callback.Policy
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class Policy<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly List<MethodRule<Attrib>> _methods;

        public Policy()
        {
            _methods = new List<MethodRule<Attrib>>();
        }

        public void AddMethod(MethodRule<Attrib> method)
        {
            _methods.Add(method);
        }

        public MethodRule<Attrib> Match(Attrib definition, MethodInfo method)
        {
            return _methods.FirstOrDefault(rule => rule.Matches(definition, method));
        }
    }
}
