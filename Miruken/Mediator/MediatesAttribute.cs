namespace Miruken.Mediator
{
    using Callback;
    using Callback.Policy;

    public class MediatesAttribute : DefinitionAttribute
    {
        public MediatesAttribute()
        {          
        }

        public MediatesAttribute(object key)
        {
            Key = key;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly CallbackPolicy Policy =
            ContravariantPolicy.Create<Command>(r => r.Callback,
                x => x.MatchMethod(x.Target.OfType<IRequest>(),
                                   x.Composer.Optional)
                      .MatchMethod(Return.Is("resp"),
                                   x.Target.OfType(typeof(IRequest<>), "resp"),
                                   x.Composer.Optional)
                      .MatchMethod(x.Target.OfType<INotification>(),
                                   x.Composer.Optional)
                      .MatchMethod(x.Callback, x.Composer.Optional)
                      .UseTargetFilters()
            );
    }
}
