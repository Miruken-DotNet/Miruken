namespace Miruken.Callback
{
    using System;
    using System.Reflection;
    using Policy;

    public class HandlesAttribute : ContravariantAttribute
    {
        private MethodRule<HandlesAttribute> _methodRule;

        public HandlesAttribute()
        {
        }

        public HandlesAttribute(object key)
        {
            Key = key;
        }

        protected override void Match(MethodInfo method)
        {
            _methodRule = Policy.Match(this, method);
            if (_methodRule == null)
                throw new InvalidOperationException(
                    $"Policy for {GetType().FullName} rejected method {method.ReflectedType?.FullName}:{method.Name}");
        }

        protected override object[] ResolveArgs(object callback, IHandler composer)
        {
            return _methodRule.ResolveArgs(this, callback, composer);
        }

        protected override bool Dispatched(object result)
        {
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
