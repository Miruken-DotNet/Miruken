namespace Miruken.Map
{
    using Callback;
    using Callback.Policy;

    public class MapsFromAttribute : DefinitionAttribute
    {
        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly CallbackPolicy Policy =
            ContravariantPolicy.Create<MapFrom>(m => m.Source,
                x => x.MatchCallbackMethod(x.Target, x.Extract(v => v.Format))
                      .MatchMethod(x.Target, x.Callback)
                      .MatchMethod(x.Callback)
            );
    }
}
