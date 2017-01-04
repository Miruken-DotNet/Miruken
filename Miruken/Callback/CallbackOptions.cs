namespace Miruken.Callback
{
    public abstract class CallbackOptions<T> : Composition
        where T : CallbackOptions<T>
    {
        public abstract void MergeInto(T other);

        public OptionsHandler<T> Decorate(IHandler handler)
        {
            return handler == null ? null
                 : new OptionsHandler<T>(handler, (T)this);
        }
    }

    public class OptionsHandler<T> : HandlerDecorator
        where T : CallbackOptions<T>
    {
        private readonly T _options;

        public OptionsHandler(IHandler handler, T options)
            : base(handler)
        {
            _options = options;
        }

        [Handles]
        private void Options(T options)
        {
            _options.MergeInto(options);
        }
    }
}
