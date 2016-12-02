using System.Collections.Generic;
using System.Linq;

namespace Miruken.Callback
{
    public interface ICompositeCallbackHandler : ICallbackHandler
    {
        ICompositeCallbackHandler AddHandlers(params object[] handlers);

        ICompositeCallbackHandler InsertHandlers(int atIndex, params object[] handlers);

        ICompositeCallbackHandler RemoveHandlers(params object[] handlers);
    }

	public class CompositeCallbackHandler : CallbackHandler, ICompositeCallbackHandler
	{
        private readonly List<ICallbackHandler> _handlers = new List<ICallbackHandler>();

        public CompositeCallbackHandler(params object[] handlers)
        {
            if (handlers != null)
                AddHandlers(handlers);
        }

	    public ICallbackHandler[] Handlers
	    {
	        get { return _handlers.ToArray(); }
	    }

        public ICompositeCallbackHandler AddHandlers(params object[] handlers)
        {
            foreach (var handler in handlers
                .Where(handler => Find(handler) == null))
                _handlers.Add(handler.ToCallbackHandler());
            return this;
        }

	    public ICompositeCallbackHandler InsertHandlers(int atIndex, params object[] handlers)
	    {
	        var index = 0;
            foreach (var handler in handlers
                .Where(handler => Find(handler) == null))
                _handlers.Insert(atIndex + index++, handler.ToCallbackHandler());
	        return this;
	    }

        public ICompositeCallbackHandler RemoveHandlers(params object[] handlers)
        {
            foreach (var match in handlers
                .Select(handler => Find(handler))
                .Where(match => match != null))
                _handlers.Remove(match);
            return this;
        }

	    protected override bool HandleCallback(
            object callback, bool greedy, ICallbackHandler composer)
		{
		    var handled = false;
		    foreach (var handler in _handlers)
		    {
		        if (!handler.Handle(callback, greedy, composer)) continue;
		        if (!greedy) return true;
		        handled = true;
		    }
            return base.HandleCallback(callback, greedy, composer) || handled;                                                                                     
		}

	    private ICallbackHandler Find(object instance)
	    {
	        foreach (var handler in _handlers)
	        {
	            if (handler == instance) return handler;
	            var surrogate = handler as CallbackHandler;
	            if (surrogate != null && surrogate.Surrogate == instance)
	                return surrogate;
	        }
            return null;
	    }
	}
}
