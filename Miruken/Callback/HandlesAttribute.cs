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
                      .Create((m,r,a) => new HandlesMethod(m,r,a))
            );
        }

        private class HandlesMethod : ContravariantMethod<HandlesAttribute>
        {
            public HandlesMethod(
                MethodInfo method, MethodRule<HandlesAttribute> rule,
                HandlesAttribute attribute)
                : base(method, rule, attribute)
            {                  
            }

            protected override bool Verify(object target, object callback, 
                                            IHandler composer)
            {
                var result = Invoke(target, callback, composer);
                return result == null || true.Equals(result);
            }
        }

        private static readonly ContravariantPolicy<HandlesAttribute> Policy;
    }
}
