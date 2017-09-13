namespace Miruken.Map
{
    using System.Collections.Generic;
    using System.Linq;
    using Callback;
    using Callback.Policy;

    public class MapsAttribute : DefinitionAttribute
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
            var formats  = dispatch.Attributes
                .OfType<IFormatMatching>().ToArray();
            if (Matches(format, formats)) return true;
            var sharedFormats = dispatch.Owner.Attributes
                .OfType<IFormatMatching>().ToArray();
            if (Matches(format, sharedFormats)) return true;
            return formats.Length == 0 && sharedFormats.Length == 0;
        }

        private static bool Matches(
            object format, IEnumerable<IFormatMatching> formats)
        {
            return formats.Any(f => f.Matches(format));
        }

        public static readonly CallbackPolicy Policy =
            BivariantPolicy.Create<Mapping>(m => m.Type, m => m.Source,
                x => x.MatchCallbackMethod(x.ReturnKey, x.Target)
                      .MatchMethod(x.ReturnKey.OrVoid, x.Callback)
            );
    }
}
