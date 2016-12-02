namespace Miruken.Callback
{
    public abstract class CallbackOptions<T> : Composition
        where T : CallbackOptions<T>
    {
        public abstract void MergeInto(T other);
    }

    public class CallbackOptionsHandler<T> : CallbackHandlerDecorator
        where T : CallbackOptions<T>
    {
        private readonly T _options;

        public CallbackOptionsHandler(ICallbackHandler handler, T options)
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
