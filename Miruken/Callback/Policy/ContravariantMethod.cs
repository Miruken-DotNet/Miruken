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

        protected override bool Verify(object target, object callback, IHandler composer)
        {
            var result = Invoke(target, callback, composer);
            return result == null || true.Equals(result);
        }

        public override int CompareTo(MethodDefinition other)
        {
            var otherMethod = other as ContravariantMethod<Attrib>;
            if (otherMethod == null) return -1;
            if (otherMethod.VarianceType == VarianceType)
                return 0;
            if (VarianceType == null ||
                VarianceType.IsAssignableFrom(otherMethod.VarianceType))
                return 1;
            return -1;
        }
    }
}
