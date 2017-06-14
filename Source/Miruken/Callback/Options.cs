namespace Miruken.Callback
{
    using System;

    public abstract class Options<T> : Composition,
        IBoundCallback, IInvokeCallback
        where T : Options<T>
    {
        public object Bounds { get; set; }

        public abstract void MergeInto(T other);

        public OptionsHandler<T> Decorate(IHandler handler)
        {
            return handler == null ? null
                 : new OptionsHandler<T>(handler, (T)this);
        }
    }

    public class OptionsHandler<T> : HandlerDecorator
        where T : Options<T>
    {
        private readonly T _options;

        public OptionsHandler(IHandler handler, T options)
            : base(handler)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _options = options;
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            var composition = callback as Composition;
            var options     = (composition?.Callback ?? callback) as T;
            var handled     = options != null;
            if (handled) _options.MergeInto(options);
            return handled && !greedy ||
                (Decoratee.Handle(callback, ref greedy, composer) || handled);
        }
    }
}
