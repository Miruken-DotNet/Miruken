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
            ContravariantPolicy.Create(
                x => x.MatchMethod(x.Callback, x.Composer.Optional, x.Binding.Optional)
            );
    }
}
