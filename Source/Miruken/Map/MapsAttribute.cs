namespace Miruken.Map
{
    using System.Linq;
    using Callback;
    using Callback.Policy;

    public class MapsAttribute : CategoryAttribute
    {
        public MapsAttribute()
        {
            Strict = true;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public override bool Approve(object callback, PolicyMethodBinding binding)
        {
            var mapping = (Mapping)callback;
            var format  = mapping.Format;
            if (format == null) return true;
            var dispatch = binding.Dispatcher;
            return dispatch.Attributes.OfType<IFormatMatching>()
                       .Any(f => f.Matches(format))
                || dispatch.Owner.Attributes.OfType<IFormatMatching>()
                       .Any(f => f.Matches(format));
        }

        public static readonly CallbackPolicy Policy =
            BivariantPolicy.Create<Mapping>(m => m.Type, m => m.Source,
                x => x.MatchMethod(x.ReturnKey.Alias("to"),
                          x.Target, x.Extract(m => m.Target).Alias("to"))
                      .MatchCallbackMethod(x.ReturnKey, x.Target)
                      .MatchMethod(x.ReturnKey.OrVoid, x.Callback)
            );
    }
}
