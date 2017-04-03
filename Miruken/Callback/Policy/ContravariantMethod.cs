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
