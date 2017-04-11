namespace Miruken.Callback.Policy
{
    using System.Reflection;

    public class ContravariantMethod<Attrib> : MethodDefinition<Attrib>
        where Attrib : DefinitionAttribute
    {
        public ContravariantMethod(MethodInfo method,
                                   MethodRule<Attrib> rule,
                                   Attrib attribute)
            : base(method, rule, attribute)
        {        
        }

        protected override bool VerifyResult(object target, object callback, IHandler composer)
        {
            var result = Invoke(target, callback, composer);
            return result == null || true.Equals(result);
        }
    }
}
