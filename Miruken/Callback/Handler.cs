namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Handler : MarshalByRefObject, IHandler
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
            {
                callback = compose.Callback;
                if (callback == null) return false;
            }

            var dispatch = callback as ICallbackDispatch;
            if (dispatch != null)
                return dispatch.Dispatch(this, greedy, composer);

            return !ShouldSkipDefinitions() &&
                HandlerMetadata.Dispatch(typeof(HandlesAttribute),
                this, callback, greedy, composer);
        }

	    private bool ShouldSkipDefinitions()
	    {
	        if (Surrogate != null) return false;
	        var handlerType = GetType();
	        return handlerType == typeof(Handler) ||
	               handlerType == typeof(CallbackFilter);
	    }

        public static IHandler operator +(Handler c1, IHandler c2)
        {
            return c1.Chain(c2);
        }

        public static IHandler operator +(Handler c1, IEnumerable<IHandler> c2)
        {
            return c1.Chain(c2.ToArray());
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
