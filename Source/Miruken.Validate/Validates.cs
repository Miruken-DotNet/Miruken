namespace Miruken.Validate
{
    using System;
    using Callback;
    using Callback.Policy;

    public class Validates : CategoryAttribute
    {
        public Validates()
        {          
        }

        public Validates(Type key)
        {
            InKey = key;
        }

        public object Scope { get; set; }

        public bool SkipIfInvalid { get; set; }

        public override CallbackPolicy CallbackPolicy => Policy;

        public override bool Approve(object callback, PolicyMemberBinding binding)
        {
            var validation = (Validation)callback;
            return (validation.Outcome.IsValid || 
                   !(validation.StopOnFailure || SkipIfInvalid))
                && validation.ScopeMatcher.Matches(Scope);
        }

        public static void AddFilters(params IFilterProvider[] providers) =>
            Policy.AddFilters(providers);

        public static void AddFilters(params Type[] filterTypes) =>
            Policy.AddFilters(filterTypes);

        public static readonly CallbackPolicy Policy =
             ContravariantPolicy.Create<Validation>(v => v.Target,
                    x => x.MatchCallbackMethod(x.Target, x.Extract(v => v.Outcome))
                          .MatchMethod(x.Target, x.Callback)
                          .MatchMethod(x.Callback)
             );
    }
}
