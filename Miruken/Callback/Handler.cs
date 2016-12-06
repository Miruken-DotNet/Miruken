namespace Miruken.Callback
{
	public partial class Handler : IHandler
	{
	    public Handler()
	    {        
	    }

	    public Handler(object surrogate)
	    {
	        Surrogate = surrogate;
        }

	    public object Surrogate { get; }

	    public virtual bool Handle(
            object callback, bool greedy = false, IHandler composer = null)
	    {
            if (callback == null) return false;
            if (composer == null)
                composer = new CompositionScope(this);
	        return HandleCallback(callback, greedy, composer);
	    }

        protected virtual bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var compose = callback as Composition;
            if (compose != null)
                callback = compose.Callback;

            return callback != null & 
                 ( TryHandleMethod(callback, greedy, composer)
                || TryResolveMethod(callback, composer)
                || TryDefinitions(callback, greedy, composer));
        }

        private bool TryDefinitions(object callback, bool greedy, IHandler composer)
	    {
            var handled    = false;
            var resolution = callback as Resolution;

            // Try implicit resolution first

            if (resolution != null)
            {
                handled = (Surrogate != null && resolution.TryResolve(Surrogate, false));
                if (!handled || greedy)
                    handled = resolution.TryResolve(this, false) || handled;
                if (handled && !greedy) return true;
            }

            if (ShouldShortCircuitDefinitions(callback))
                return handled;

            var definition = resolution != null
                           ? typeof(ProvidesAttribute)
                           : typeof(HandlesAttribute);

            if (Surrogate != null)
            {
                var descriptor = HandlerMetadata.GetDescriptor(Surrogate.GetType());
                handled = descriptor.Dispatch(definition, Surrogate, callback, greedy, composer) || handled;
            }

            if (!handled || greedy)
            {
                var descriptor = HandlerMetadata.GetDescriptor(GetType());
                handled = descriptor.Dispatch(definition, this, callback, greedy, composer) || handled;
            }

            return handled;
	    }

	    private bool ShouldShortCircuitDefinitions(object callback)
	    {
	        if ((callback is HandleMethod) || (callback is ResolveMethod))
	            return true;

	        if (Surrogate != null)
	            return false;

	        var handlerType = GetType();
	        return handlerType == typeof(Handler) ||
	               handlerType == typeof(CallbackFilter);
	    }

        public static IHandler operator +(Handler c1, IHandler c2)
        {
            return c1.Chain(c2);
        }
	}

    public class CompositionScope : HandlerDecorator
    {
        public CompositionScope(IHandler handler)
            : base(handler)
        {
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            if (callback.GetType() != typeof(Composition))
                callback = new Composition(callback);
            return base.HandleCallback(callback, greedy, composer);
        }
    }
}
