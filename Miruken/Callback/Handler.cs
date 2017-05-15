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
	            composer = this as CompositionScope ?? new CompositionScope(this);
	        return HandleCallback(callback, greedy, composer);
	    }

        protected virtual bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            if (Surrogate == null && SkippedTypes.Contains(GetType()))
                return false;

            var compose = callback as Composition;
            if (compose != null)
            {
                callback = compose.Callback;
                if (callback == null) return false;
            }

            var dispatch = callback as IDispatchCallback;
            return dispatch?.Dispatch(this, greedy, composer)
                ?? HandlesAttribute.Policy.Dispatch(this, callback, greedy, composer);
        }

        public static CascadeHandler operator +(Handler c1, IHandler c2)
        {
            return new CascadeHandler(c1, c2);
        }

	    public static CompositeHandler operator +(Handler c1, IEnumerable<IHandler> c2)
        {
            var rest = c2.ToArray();
            var h    = new object[rest.Length + 1];
            h[0] = c1;
            rest.CopyTo(h, 1);
            return new CompositeHandler(h);
        }

        private static readonly HashSet<Type> SkippedTypes = new HashSet<Type>
        {
            typeof(Handler), typeof(HandlerFilter), typeof(CascadeHandler),
            typeof(CompositeHandler), typeof(CompositionScope)
        };
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
