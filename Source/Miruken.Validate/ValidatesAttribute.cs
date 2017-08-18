namespace Miruken.Validate
{
    using System;
    using Callback;
    using Callback.Policy;

    public class ValidatesAttribute : DefinitionAttribute
    {
        public ValidatesAttribute()
        {          
        }

        public ValidatesAttribute(Type key)
        {
            Key = key;
        }

        public object Scope { get; set; }

        public bool SkipIfInvalid { get; set; }

        public override CallbackPolicy CallbackPolicy => Policy;

        public override bool Approve(object callback, PolicyMethodBinding binding)
        {
            var validation = (Validation)callback;
            return (validation.Outcome.IsValid || 
                   !(validation.StopOnFailure || SkipIfInvalid))
                && validation.ScopeMatcher.Matches(Scope);
        }

        public static readonly CallbackPolicy Policy =
             ContravariantPolicy.Create<Validation>(v => v.Target,
                    x => x.MatchMethodWithCallback(x.Target, x.Extract(v => v.Outcome))
                          .MatchMethod(x.Target, x.Callback)
                          .MatchMethod(x.Callback)
             );
    }
}
