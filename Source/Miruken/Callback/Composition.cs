namespace Miruken.Callback
{
    public class Composition : Trampoline, IInferCallback,
          IFilterCallback, IBatchCallback, ICallbackKey
    {
        public Composition(object callback)
            : base(callback)
        {
        }

        protected Composition()
        {
        }

        object ICallbackKey.Key =>
            (Callback as ICallbackKey)?.Key;

        bool IFilterCallback.CanFilter =>
            (Callback as IFilterCallback)?.CanFilter != false;

        bool IBatchCallback.CanBatch =>
            (Callback as IBatchCallback)?.CanBatch != false;

        object IInferCallback.InferCallback()
        {
            var infer = Inference.Get(Callback);
            return ReferenceEquals(infer, Callback) ? this
                 : new Composition(infer);
        }

        public static bool IsComposed<T>(object callback)
            where T : class
        {
            var composition = callback as Composition;
            return composition?.Callback is T;
        }
    }
}
