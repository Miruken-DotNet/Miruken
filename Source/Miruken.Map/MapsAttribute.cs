namespace Miruken.Map
{
    using System;
    using System.Linq;
    using Callback;
    using Callback.Policy;

    public class MapsAttribute : DefinitionAttribute
    {
        public override CallbackPolicy CallbackPolicy => Policy;

        public object Format { get; set; }

        public override bool Approve(object callback, PolicyMethodBinding binding)
        {
            var mapsFrom = (MapFrom)callback;
            var format   = mapsFrom.Format;
            var dispatch = binding.Dispatcher;
            if (Format != null)
                return Equals(format, Format);
           var shared = dispatch.Owner.Attributes
                .OfType<IFormatMatching>()
                .SingleOrDefault();
            if (shared != null)
                return shared.Matches(format);
            return format as Type == dispatch.LogicalReturnType;
        }

        public static readonly CallbackPolicy Policy =
            ContravariantPolicy.Create<MapFrom>(m => m.Source,
                x => x.MatchCallbackMethod(x.Target, x.Extract(v => v.Format))
                      .MatchCallbackMethod(x.Target)
            );
    }
}
