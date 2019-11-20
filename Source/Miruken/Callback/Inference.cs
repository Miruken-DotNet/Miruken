namespace Miruken.Callback
{
    using System.Linq;
    using System.Threading;
    using Policy;

    public class Inference : Trampoline, IInferCallback
    {
        private Resolving[] _inferred;

        public Inference(object callback) : base(callback)
        {
        }

        object IInferCallback.InferCallback()
        {
            return this;
        }

        public override bool Dispatch(object handler,
            ref bool greedy, IHandler composer)
        {
            var handled = base.Dispatch(handler, ref greedy, composer);
            if (handled) return true;
            LazyInitializer.EnsureInitialized(ref _inferred, GetInferred);
            foreach (var infer in _inferred)
            {
                if (ReferenceEquals(infer.Key, handler.GetType())) continue;
                if (!infer.Dispatch(handler, ref greedy, composer)) continue;
                if (!greedy) return true;
                handled = true;
            }
            return handled;
        }

        private Resolving[] GetInferred() =>
            CallbackPolicy.GetInstanceHandlers(Callback)
                .Select(handler => new Resolving(handler.HandlerType, Callback))
                .ToArray();

        public static object Get(object callback)
        {
            return callback is IInferCallback infer
                ? infer.InferCallback()
                : new Inference(callback);
        }
    }
}
