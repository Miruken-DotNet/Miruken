using System.Collections.Generic;
using System.Linq;

namespace Miruken.Callback
{
    using System.Collections;

    public interface ICompositeHandler : IHandler
    {
        T FindHandler<T>();

        ICompositeHandler AddHandlers(IEnumerable handlers);

        ICompositeHandler AddHandlers(params object[] handlers);

        ICompositeHandler InsertHandlers(int atIndex, IEnumerable handlers);

        ICompositeHandler InsertHandlers(int atIndex, params object[] handlers);

        ICompositeHandler RemoveHandlers(IEnumerable handlers);

        ICompositeHandler RemoveHandlers(params object[] handlers);
    }

    [Unmanaged]
	public class CompositeHandler : Handler, ICompositeHandler
	{
        private readonly List<IHandler> _handlers = new List<IHandler>();

        public CompositeHandler(params object[] handlers)
        {
            if (handlers != null)
                AddHandlers(handlers);
        }

	    public IHandler[] Handlers => _handlers.ToArray();

	    public ICompositeHandler AddHandlers(IEnumerable handlers)
        {
            foreach (var handler in handlers.Cast<object>()
                .Where(h => h != null && FindHandler(h) == null))
                _handlers.Add(ToHandler(handler));
            return this;
        }

	    public ICompositeHandler AddHandlers(params object[] handlers)
	    {
	        return AddHandlers((IEnumerable)handlers);
	    }

        public ICompositeHandler InsertHandlers(int atIndex, IEnumerable handlers)
	    {
	        var index = 0;
            foreach (var handler in handlers.Cast<object>()
                .Where(h => h != null && FindHandler(h) == null))
                _handlers.Insert(atIndex + index++, ToHandler(handler));
	        return this;
	    }

	    public ICompositeHandler InsertHandlers(int atIndex, params object[] handlers)
	    {
	        return InsertHandlers(atIndex, (IEnumerable)handlers);
	    }

        public ICompositeHandler RemoveHandlers(IEnumerable handlers)
        {
            foreach (var match in handlers.Cast<object>()
                .Select(FindHandler)
                .Where(match => match != null))
                _handlers.Remove(match);
            return this;
        }

	    public ICompositeHandler RemoveHandlers(params object[] handlers)
	    {
            return RemoveHandlers((IEnumerable)handlers);
	    }

        public T FindHandler<T>()
	    {
	        foreach (var handler in _handlers)
	        {
	            switch (handler)
	            {
	                case T t: return t;
	                case HandlerAdapter adapter when adapter.Handler is T t:
	                    return t;
	            }
	        }
	        return default;
	    }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
		{
            var handled = base.HandleCallback(callback, ref greedy, composer);
            if (handled && !greedy) return true;
		    foreach (var handler in Handlers)
		    {
		        if (!handler.Handle(callback, ref greedy, composer)) continue;
		        if (!greedy) return true;
		        handled = true;
		    }
            return handled;                                                                                     
		}

	    private IHandler FindHandler(object instance)
	    {
	        foreach (var handler in _handlers)
	        {
	            if (handler == instance) return handler;
	            if (handler is HandlerAdapter adapter && adapter.Handler == instance)
	                return adapter;
	        }
	        return null;
	    }
    }
}
