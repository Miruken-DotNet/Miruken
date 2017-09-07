namespace Miruken.Map
{
    using System.Linq;
    using Callback;
    using Callback.Policy;

    public class MapsToAttribute : DefinitionAttribute
    {
        public override CallbackPolicy CallbackPolicy => Policy;

        public override bool Approve(object callback, PolicyMethodBinding binding)
        {
            var mapsFrom = (MapTo)callback;
            var format   = mapsFrom.Format;
            var dispatch = binding.Dispatcher;
            if (dispatch.Attributes.OfType<IFormatMatching>()
                .Any(f => f.Matches(format))) return true;
            return dispatch.Owner.Attributes.OfType<IFormatMatching>()
                .Any(f => f.Matches(format));
        }

        public static readonly CallbackPolicy Policy =
            ContravariantPolicy.Create<MapTo>(m => m.TypeOrInstance,
                x => x.MatchCallbackMethod(x.Target, x.Extract(v => v.Format))
                      .MatchMethod(x.Target, x.Callback)
                      .MatchMethod(x.Callback)
            );
    }
}
