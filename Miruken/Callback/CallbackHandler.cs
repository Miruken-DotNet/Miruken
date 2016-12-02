using System;

namespace Miruken.Callback
{
	public partial class CallbackHandler : ICallbackHandler
	{
	    private readonly object _surrogate;

	    public CallbackHandler()
	    {        
	    }

	    public CallbackHandler(object surrogate)
	    {
	        _surrogate = surrogate;
        }

	    public Object Surrogate
	    {
	        get { return _surrogate; }
	    }

	    public virtual bool Handle(object callback, bool greedy, ICallbackHandler composer)
	    {
            if (callback == null) return false;
            if (composer == null)
                composer = new CompositionScope(this);
	        return HandleCallback(callback, greedy, composer);
	    }

        protected virtual bool HandleCallback(object callback, bool greedy, ICallbackHandler composer)
        {
            var compose = callback as Composition;
            if (compose != null)
                callback = compose.Callback;

            return callback != null & 
                 ( TryHandleMethod(callback, greedy, composer)
                || TryResolveMethod(callback, composer)
                || TryDefinitions(callback, greedy, composer));
        }

        private bool TryDefinitions(object callback, bool greedy, ICallbackHandler composer)
	    {
            var handled    = false;
            var resolution = callback as Resolution;

            // Try implicit resolution first

            if (resolution != null)
            {
                handled = (_surrogate != null && resolution.TryResolve(_surrogate, false));
                if (!handled || greedy)
                    handled = resolution.TryResolve(this, false) || handled;
                if (handled && !greedy) return true;
            }

            if (ShouldShortCircuitDefinitions(callback))
                return handled;

            var definition = resolution != null
                           ? typeof(ProvidesAttribute)
                           : typeof(HandlesAttribute);

            if (_surrogate != null)
            {
                var descriptor = CallbackHandlerMetadata.GetDescriptor(_surrogate.GetType());
                handled = descriptor.Dispatch(definition, _surrogate, callback, greedy, composer) || handled;
            }

            if (!handled || greedy)
            {
                var descriptor = CallbackHandlerMetadata.GetDescriptor(GetType());
                handled = descriptor.Dispatch(definition, this, callback, greedy, composer) || handled;
            }

            return handled;
	    }

	    private bool ShouldShortCircuitDefinitions(object callback)
	    {
	        if ((callback is HandleMethod) || (callback is ResolveMethod))
	            return true;

	        if (_surrogate != null)
	            return false;

	        var handlerType = GetType();
	        return handlerType == typeof(CallbackHandler) ||
	               handlerType == typeof(CallbackFilter);
	    }

        public static ICallbackHandler operator +(CallbackHandler c1, ICallbackHandler c2)
        {
            return c1.Chain(c2);
        }
	}

    public class CompositionScope : CallbackHandlerDecorator
    {
        public CompositionScope(ICallbackHandler handler)
            : base(handler)
        {
        }

        protected override bool HandleCallback(
            object callback, bool greedy, ICallbackHandler composer)
        {
            if (callback.GetType() != typeof(Composition))
                callback = new Composition(callback);
            return base.HandleCallback(callback, greedy, composer);
        }
    }
}
