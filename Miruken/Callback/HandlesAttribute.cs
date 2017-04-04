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

        public override MethodDefinition Match(MethodInfo method)
        { 
            return Policy.Match(method, this);
        }

        static HandlesAttribute()
        {
            Policy = ContravariantPolicy.For<HandlesAttribute>(
                x => x.MatchMethod(x.Callback)
                      .MatchMethod(x.Callback, x.Composer)
            );
        }

        private static readonly ContravariantPolicy<HandlesAttribute> Policy;
    }
}
