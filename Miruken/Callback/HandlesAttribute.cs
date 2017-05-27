namespace Miruken.Callback
{
    using Policy;

    public class HandlesAttribute : DefinitionAttribute
    {
        public HandlesAttribute()
        {
        }

        public HandlesAttribute(object key)
        {
            Key = key;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly CallbackPolicy Policy =
            ContravariantPolicy.Create<Command>(r => r.Callback,
                x => x.MatchMethod(x.Target,   x.Composer.Optional, x.Binding.Optional)
                      .MatchMethod(x.Callback, x.Composer.Optional, x.Binding.Optional)
            );
    }
}
