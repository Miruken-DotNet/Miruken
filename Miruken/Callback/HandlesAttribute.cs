namespace Miruken.Callback
{
    using System.Reflection;
    using Policy;

    public class HandlesAttribute : DefinitionAttribute
    {
        public HandlesAttribute()
        {
        }

        public HandlesAttribute(object key)
        {
            Key = key;
        }

        public override CallbackPolicy MethodPolicy => Policy;

        public override MethodDefinition MatchMethod(MethodInfo method)
        { 
            return Policy.MatchMethod(method, this);
        }

        public static readonly ContravariantPolicy<HandlesAttribute> Policy =
            ContravariantPolicy.For<HandlesAttribute>(
                x => x.MatchMethod(x.Callback, x.Composer.Optional)
            );
    }
}
