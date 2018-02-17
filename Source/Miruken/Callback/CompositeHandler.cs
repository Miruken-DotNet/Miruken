using System.Collections.Generic;
using System.Linq;

namespace Miruken.Callback
{
    public interface ICompositeHandler : IHandler
    {
        ICompositeHandler AddHandlers(params object[] handlers);

        ICompositeHandler InsertHandlers(int atIndex, params object[] handlers);

        ICompositeHandler RemoveHandlers(params object[] handlers);
    }

	public class CompositeHandler : Handler, ICompositeHandler
	{
        private readonly List<IHandler> _handlers = new List<IHandler>();

        public CompositeHandler(params object[] handlers)
        {
            if (handlers != null)
                AddHandlers(handlers);
        }

	    public IHandler[] Handlers => _handlers.ToArray();

	    public ICompositeHandler AddHandlers(params object[] handlers)
        {
            foreach (var handler in handlers
                .Where(h => h != null && Find(h) == null))
                _handlers.Add(ToHandler(handler));
            return this;
        }

	    public ICompositeHandler InsertHandlers(int atIndex, params object[] handlers)
	    {
	        var index = 0;
            foreach (var handler in handlers
                .Where(h => h != null && Find(h) == null))
                _handlers.Insert(atIndex + index++, ToHandler(handler));
	        return this;
	    }

        public ICompositeHandler RemoveHandlers(params object[] handlers)
        {
            foreach (var match in handlers
                .Select(Find)
                .Where(match => match != null))
                _handlers.Remove(match);
            return this;
        }

	    protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
		{
            var handled = base.HandleCallback(callback, ref greedy, composer);
            if (handled && !greedy) { return true; }
		    foreach (var handler in _handlers)
		    {
		        if (!handler.Handle(callback, ref greedy, composer)) continue;
		        if (!greedy) return true;
		        handled = true;
		    }
            return handled;                                                                                     
		}

	    private IHandler Find(object instance)
	    {
	        foreach (var handler in _handlers)
	        {
	            if (handler == instance) return handler;
	            var adapter = handler as HandlerAdapter;
	            if (adapter != null && adapter.Handler == instance)
	                return adapter;
	        }
            return null;
	    }
	}
}
