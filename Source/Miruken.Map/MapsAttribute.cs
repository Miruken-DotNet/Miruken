namespace Miruken.Map
{
    using System;
    using Callback;
    using Callback.Policy;

    public class MapsAttribute : DefinitionAttribute
    {
        public MapsAttribute()
        {         
        }

        public MapsAttribute(Type key)
        {
            InKey = key;
        }

        public object Format
        {
            get { return OutKey; }
            set { OutKey = value; }
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly CallbackPolicy Policy =
            BivariantPolicy.Create<Mapping>(m => m.Format, m => m.Source,
                x => x.MatchCallbackMethod(x.ReturnKey, x.Target, x.Extract(v => v.Format))
                      .MatchCallbackMethod(x.ReturnKey, x.Target)
                      .MatchMethod(x.ReturnKey.OrVoid, x.Callback)
            );
    }
}
