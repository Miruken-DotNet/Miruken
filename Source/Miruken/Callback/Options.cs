namespace Miruken.Callback
{
    using System;

    public abstract class Options<T> : Composition,
        IBoundCallback, IInferCallback, IFilterCallback, IBatchCallback
        where T : Options<T>
    {
        public object Bounds { get; set; }

        public abstract void MergeInto(T other);

        public OptionsHandler<T> Decorate(IHandler handler)
        {
            return handler == null ? null
                 : new OptionsHandler<T>(handler, (T)this);
        }

        bool IFilterCallback.CanFilter => false;
        bool IBatchCallback.CanBatch => false;

        object IInferCallback.InferCallback()
        {
            return this;
        }
    }

    public class OptionsHandler<T> : DecoratedHandler
        where T : Options<T>
    {
        private readonly T _options;

        public OptionsHandler(IHandler handler, T options)
            : base(handler)
        {
            _options = options 
                ?? throw new ArgumentNullException(nameof(options));
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            var composition = callback as Composition;
            var options     = (composition?.Callback ?? callback) as T;
            var handled     = options != null;
            if (handled) _options.MergeInto(options);
            return handled && !greedy ||
                Decoratee.Handle(callback, ref greedy, composer) || handled;
        }
    }

    public static class OptionExtensions
    {
        public static T GetOptions<T>(this IHandler handler, T options)
            where T : Options<T>
        {
            return handler == null || options == null ? null 
                 : handler.Handle(options, true) ? options : null;
        }

        public static T GetOptions<T>(this IHandler handler) 
            where T : Options<T>, new()
        {
            if (handler == null) return null;
            var options = new T();
            return handler.Handle(options, true) ? options : null;
        }
    }
}
