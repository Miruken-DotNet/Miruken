namespace Miruken.Callback
{
    using System;

    public class Composition
        : Trampoline, ICallback, IInferCallback,
          IFilterCallback, IBatchCallback,
          ICallbackKey
    {
        public Composition(object callback)
            : base(callback)
        {
        }

        protected Composition()
        {
        }

        public Type ResultType
        {
            get
            {
                var cb = Callback as ICallback;
                return cb?.ResultType;
            }
        }

        public object Result
        {
            get
            {
                var cb = Callback as ICallback;
                return cb?.Result;
            }

            set
            {
                if (Callback is ICallback cb)
                    cb.Result = value;
            }
        }

        object ICallbackKey.Key =>
            (Callback as ICallbackKey)?.Key;

        bool IFilterCallback.CanFilter =>
            (Callback as IFilterCallback)?.CanFilter != false;

        bool IBatchCallback.CanBatch =>
            (Callback as IBatchCallback)?.CanBatch != false;

        object IInferCallback.InferCallback()
        {
            var infer = (Callback as IInferCallback)?.InferCallback();
            return ReferenceEquals(infer, Callback) ? this
                 : new Composition(infer ?? Inference.Get(Callback));
        }

        public static bool IsComposed<T>(object callback)
            where T : class
        {
            var composition = callback as Composition;
            return composition?.Callback is T;
        }
    }
}
