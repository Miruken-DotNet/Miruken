namespace Miruken.Map
{
    using System;
    using System.Linq;
    using Callback;
    using Callback.Policy;

    public class MapsFromAttribute : DefinitionAttribute
    {
        public override CallbackPolicy CallbackPolicy => Policy;

        public override bool Approve(object callback, PolicyMethodBinding binding)
        {
            var mapsFrom = (MapFrom)callback;
            var format   = mapsFrom.Format;
            var dispatch = binding.Dispatcher;
            var formats  = dispatch.Attributes
                .OfType<IFormatMatching>().ToArray();
            if (formats.Any(f => f.Matches(format)))
                return true;
            var sharedFormats = dispatch.Owner.Attributes
                .OfType<IFormatMatching>() .ToArray();
            if (sharedFormats.Any(f => f.Matches(format)))
                return true;
            return formats.Length == 0 && sharedFormats.Length == 0
                && format as Type == dispatch.ReturnType;
        }

        public static readonly CallbackPolicy Policy =
            ContravariantPolicy.Create<MapFrom>(m => m.Source,
                x => x.MatchCallbackMethod(x.Target, x.Extract(v => v.Format))
                      .MatchCallbackMethod(x.Target)
            );
    }
}
