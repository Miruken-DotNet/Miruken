namespace Miruken.Callback
{
    using System;
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

        public override bool Validate(
            object callback, IHandler composer, Func<object> dispatch)
        {
            var result = dispatch();
            return result == null || true.Equals(result);
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
