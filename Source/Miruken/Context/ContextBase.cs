using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Miruken.Callback;
using Miruken.Graph;

namespace Miruken.Context
{
	public abstract class ContextBase<TContext> 
        : CompositeHandler, IContext<TContext>
		where TContext : class, IContext<TContext>
	{
	    private EventHandlerList _events;
	    private readonly List<TContext> _children;
 
		protected ContextBase()
		{
            State     = ContextState.Active;
            _children = new List<TContext>();
            _events   = new EventHandlerList();
		}

		protected ContextBase(TContext parent) : this()
		{
			Parent = parent;
		}

	    public ContextState State { get; private set; }

	    ITraversing ITraversing.Parent => Parent;

	    public TContext Parent { get; }

	    public bool HasChildren => _children.Count > 0;

	    public TContext Root
        {
            get
            {
                var root = this as TContext;
                while (root?.Parent != null)
                    root = root.Parent;
                return root;                  
            }
        }

	    public TContext CreateChild()
	    {
            AssertActive();
	        var child = InternalCreateChild();
	        child.ContextEnding += ctx => Raise(ContextEvents.ChildContextEnding, ctx);
            child.ContextEnded += ctx => {
                _children.Remove(ctx);
                Raise(ContextEvents.ChildContextEnded, ctx);
            };
            _children.Add(child);
	        return child;
	    }

        ITraversing[] ITraversing.Children
        {
            get
            {
                return _children.Select(c => c as ITraversing).ToArray();
            }
        }

	    public TContext[] Children => _children.ToArray();

	    protected abstract TContext InternalCreateChild();

	    public void Store(object data)
	    {
	        AddHandlers(data);
	    }

	    protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
	    {
	        var handled = base.HandleCallback(callback, ref greedy, composer);
	        if (handled && !greedy)
	            return true;
	        var parent = Parent;
	        if (parent != null)
	            handled = handled | parent.Handle(callback, ref greedy, composer);
	        return handled;
	    }

	    public virtual bool Handle(
            TraversingAxis axis, object callback, ref bool greedy, IHandler composer)
	    {
            if (axis == TraversingAxis.Self)                                                                                                              
                return base.HandleCallback(callback, ref greedy, composer);

	        var g = greedy;
	        var handled = false;                                                                                    
            Traverse(axis, node =>
            {                                                                                                           
                handled = handled | (node == this                                                                                                   
                        ? BaseHandle(callback, ref g, composer)                                                                                             
                        : ((TContext)node).Handle(
                            TraversingAxis.Self, callback, ref g, composer));                                                                
                return handled && !g;                                                                                                                  
            });
	        greedy = g;
	        return handled;
	    }

        private bool BaseHandle(object callback, ref bool greedy, IHandler composer)
        {
            return base.HandleCallback(callback, ref greedy, composer);
        }

        public void Traverse(TraversingAxis axis, Visitor visitor)
        {
            TraversingHelper.Traverse(this, axis, visitor);
        }

	    public TContext UnwindToRoot()
	    {
            var current = this as TContext;
            while (current != null)
            {
                var parent = current.Parent;
                if (parent == null)
                {
                    current.Unwind();
                    return current;
                }
                current = parent;
            }
            return this as TContext;       
	    }

	    public virtual TContext Unwind()
	    {
            foreach (var child in Children)
                child.End();
	        return this as TContext;
	    }

	    public virtual void End()
	    {
	        if (State != ContextState.Active)
	            return;

            State = ContextState.Ending;
            Raise(ContextEvents.ContextEnding, this as TContext);

            try
            {
                Unwind();
                InternalEnd();
            }
            finally
            {
                State = ContextState.Ended;
                Raise(ContextEvents.ContextEnded, this as TContext);
                _events.Dispose();
                _events = null;
            }	        
	    }

	    protected virtual void InternalEnd()
	    {
	    }

        #region Events

        public event Action<TContext> ContextEnding
        {
            add
            {
                if (State == ContextState.Active)
                    _events.AddHandler(ContextEvents.ContextEnding, value);
                else if (State == ContextState.Ending)
                    value(this as TContext);
            }
            remove
            {
                _events.RemoveHandler(ContextEvents.ContextEnding, value);
            }
        } 

        public event Action<TContext> ContextEnded
        {
            add
            {
                if (State == ContextState.Active)
                    _events.AddHandler(ContextEvents.ContextEnded, value);
                else if (State == ContextState.Ended)
                    value(this as TContext);
            }
            remove
            {
                _events.RemoveHandler(ContextEvents.ContextEnded, value);
            }            
        }

        public event Action<TContext> ChildContextEnding
        {
            add
            {
                _events?.AddHandler(ContextEvents.ChildContextEnding, value);
            }
            remove
            {
                _events.RemoveHandler(ContextEvents.ChildContextEnding, value);
            }
        }

        public event Action<TContext> ChildContextEnded
        {
            add
            {
                _events?.AddHandler(ContextEvents.ChildContextEnded, value);
            }
            remove
            {
                _events.RemoveHandler(ContextEvents.ChildContextEnded, value);
            }
        }

        #endregion

        #region IDisposable

        protected bool IsDisposed { get; private set; }

	    void IDisposable.Dispose()
		{
	        if (IsDisposed) return;
	        Dispose(true);
	        IsDisposed = true;
	        GC.SuppressFinalize(this);
		}

		~ContextBase()
		{
			Dispose(false);
			IsDisposed = true;
		}

		protected virtual void Dispose(bool managed)
		{
            End();
		}

		protected void RequireNotDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		#endregion

        private void Raise(object key, TContext context)
        {
            var eventHandler = (Action<TContext>) _events?[key];
            eventHandler?.Invoke(context);
        }

        private void AssertActive()
        {
            if (State != ContextState.Active)
                throw new Exception("The context has already ended");
        }
	}

    internal static class ContextEvents
    {
        public static readonly object ContextEnding      = new object();
        public static readonly object ContextEnded       = new object();
        public static readonly object ChildContextEnding = new object();
        public static readonly object ChildContextEnded  = new object();
    }
}
