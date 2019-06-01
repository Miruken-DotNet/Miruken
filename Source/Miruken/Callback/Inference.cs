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

        public object InferCallback()
        {
            return this;
        }

        public override bool Dispatch(object handler, 
            ref bool greedy, IHandler composer)
        {
            var handled = base.Dispatch(handler, ref greedy, composer);
            if (handled) return true;
            LazyInitializer.EnsureInitialized(ref _inferred, CreateInferred);
            foreach (var infer in _inferred)
            {
                if (!infer.Dispatch(handler, ref greedy, composer)) continue;
                if (!greedy) return true;
                handled = true;
            }
            return handled;
        }

        private Resolving[] CreateInferred() =>
            CallbackPolicy.GetCallbackHandlers(Callback)
                .Select(handler => new Resolving(handler.HandlerType, Callback))
                .ToArray();

        public static object Get(object callback) => new Inference(callback);
    }

    public sealed class InferDecorator : DecoratedHandler
    {
        public InferDecorator(IHandler handler) : base(handler)
        {         
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            var inference = GetInference(callback);
            return Decoratee.Handle(inference, greedy, composer);
        }

        private static object GetInference(object callback)
        {
            return callback is IInferCallback infer
                 ? infer.InferCallback()
                 : Inference.Get(callback);
        }
    }
}
