namespace Miruken.Callback
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Handler : MarshalByRefObject, IHandler
	{
	    public virtual bool Handle(
            object callback, ref bool greedy, IHandler composer = null)
	    {
            if (callback == null) return false;
	        if (composer == null)
	            composer = this as CompositionScope ?? new CompositionScope(this);
	        return HandleCallback(Inference.Get(callback), ref greedy, composer);
	    }

        protected virtual bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            return Dispatch(this, callback, ref greedy, composer);
        }

	    public static bool Dispatch(
	        object handler, object callback, ref bool greedy, IHandler composer)
	    {
	        var type = handler as Type ?? handler?.GetType()
	            ?? throw new ArgumentNullException(nameof(handler));
	        if (SkipTypes.Contains(type)) return false;
            var dispatch = callback as IDispatchCallback ?? new Command(callback);
	        return dispatch.Dispatch(handler, ref greedy, composer);
	    }

	    public static CascadeHandler operator +(Handler h1, object h2)
        {
            return new CascadeHandler(h1, h2);
        }

	    public static CompositeHandler operator +(Handler h1, IEnumerable handlers)
        {
            var rest = handlers.Cast<object>().ToArray();
            var h    = new object[rest.Length + 1];
            h[0] = h1;
            rest.CopyTo(h, 1);
            return new CompositeHandler(h);
        }

        protected static IHandler ToHandler(object instance)
        {
            if (instance == null) return null;
            return instance as IHandler ?? new HandlerAdapter(instance);
        }

        private static readonly HashSet<Type> SkipTypes = new HashSet<Type>
        {
            typeof(Handler), typeof(FilteredHandler), typeof(CascadeHandler),
            typeof(CompositeHandler), typeof(CompositionScope)
        };
	}

    public class CompositionScope : DecoratedHandler
    {
        public CompositionScope(IHandler handler)
            : base(handler)
        {
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            if (callback.GetType() != typeof(Composition))
                callback = new Composition(callback);
            return base.HandleCallback(callback, ref greedy, composer);
        }
    }
}
