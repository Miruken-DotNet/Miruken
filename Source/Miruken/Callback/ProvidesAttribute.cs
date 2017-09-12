namespace Miruken.Callback
{
    using System;
    using Policy;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = true, Inherited = false)]
    public class ProvidesAttribute : DefinitionAttribute
    {
        public ProvidesAttribute()
        {
        }

        public ProvidesAttribute(object key)
        {
            OutKey = key;
        }

        public ProvidesAttribute(string key, StringComparison comparison)
        {
            OutKey = new StringKey(key, comparison);
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly CallbackPolicy Policy =
             CovariantPolicy.Create<Inquiry>(r => r.Key,
                x => x.MatchMethod(x.ReturnKey.OrVoid, x.Callback)
                      .MatchMethod(x.ReturnKey)
                );
    }
}
