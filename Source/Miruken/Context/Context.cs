using System;
using System.Collections.Generic;
using System.ComponentModel;
using Miruken.Callback;
using Miruken.Graph;

namespace Miruken.Context
{
    using System.Threading;

    public enum ContextState
    {
        Active = 0,
        Ending,
        Ended
    }

    public class Context : CompositeHandler,
        IHandlerAxis, ITraversing, IDisposable
	{
	    private EventHandlerList _events;
	    private ReaderWriterLockSlim _lock;
	    private readonly List<Context> _children;

        public static readonly object AlreadyEnded = new object();
	    public static readonly object Unwinded     = new object();
	    public static readonly object Disposed     = new object();

        public Context()
		{
            State     = ContextState.Active;
            _children = new List<Context>();
            _events   = new EventHandlerList();
		    _lock     = new ReaderWriterLockSlim();
        }

		public Context(Context parent) : this()
		{
			Parent = parent;
		}

	    public ContextState State { get; private set; }

	    ITraversing ITraversing.Parent => Parent;

	    public Context Parent { get; }

        public bool HasChildren
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _children.Count > 0;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public Context Root
        {
            get
            {
                var root = this;
                while (root?.Parent != null)
                    root = root.Parent;
                return root;                  
            }
        }

	    ITraversing[] ITraversing.Children => Children;

	    public Context[] Children
	    {
	        get
	        {
	            _lock.EnterReadLock();
	            try
	            {
	                return _children.ToArray();
                }
	            finally
	            {
	                _lock.ExitReadLock();
	            }
	        }
	    }

        public Context CreateChild()
	    {
            AssertActive();
	        var child = InternalCreateChild();
	        child.ContextEnding += (ctx, reason) => 
	            Raise(ContextEvents.ChildContextEnding, ctx, reason);
            child.ContextEnded += (ctx, reason) => {
                _lock.EnterWriteLock();
                try
                {
                    _children.Remove(ctx);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                Raise(ContextEvents.ChildContextEnded, ctx, reason);
            };
	        _lock.EnterWriteLock();
	        try
	        {
	            _children.Add(child);
            }
	        finally
	        {
	            _lock.ExitWriteLock();
	        }
	        return child;
	    }

	    protected virtual Context InternalCreateChild()
	    {
            return new Context(this);
	    }

	    public Context Store(object data)
	    {
            if (data != null)
	            AddHandlers(data);
	        return this;
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

	    public bool Handle(TraversingAxis axis, object callback,
	        ref bool greedy, IHandler composer)
	    {
	        if (composer == null)
	            composer = new CompositionScope(this);

            if (axis == TraversingAxis.Self)
	            return base.HandleCallback(callback, ref greedy, composer);

	        var g = greedy;
	        var handled = false;                                                                                    
            Traverse(axis, node =>
            {                                                                                                           
                handled = handled | (node == this                                                                                                   
                        ? BaseHandle(callback, ref g, composer)                                                                                             
                        : ((Context)node).Handle(
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

	    public Context UnwindToRoot(object reason = null)
	    {
            var current = this;
            while (true)
            {
                var parent = current.Parent;
                if (parent == null)
                {
                    current.Unwind(reason);
                    return current;
                }
                current = parent;
            }
	    }

	    public Context Unwind(object reason = null)
	    {
            foreach (var child in Children)
                child.End(reason ?? Unwinded);
	        return this;
	    }

	    public void End(object reason = null)
	    {
	        if (State != ContextState.Active)
	            return;

            State = ContextState.Ending;
            Raise(ContextEvents.ContextEnding, this, reason);

            try
            {
                Unwind();
                InternalEnd(reason);
            }
            finally
            {
                State = ContextState.Ended;
                Raise(ContextEvents.ContextEnded, this, reason);
                _events.Dispose();
                _lock.Dispose();
                _events = null;
                _lock   = null;
            }	        
	    }

	    protected virtual void InternalEnd(object reason)
	    {
	    }

        #region Events

        public event Action<Context, object> ContextEnding
        {
            add
            {
                switch (State)
                {
                    case ContextState.Active:
                        _events.AddHandler(ContextEvents.ContextEnding, value);
                        break;
                    case ContextState.Ending:
                        value(this, AlreadyEnded);
                        break;
                }
            }
            remove => _events.RemoveHandler(ContextEvents.ContextEnding, value);
        } 

        public event Action<Context, object> ContextEnded
        {
            add
            {
                switch (State)
                {
                    case ContextState.Active:
                        _events.AddHandler(ContextEvents.ContextEnded, value);
                        break;
                    case ContextState.Ended:
                        value(this, AlreadyEnded);
                        break;
                }
            }
            remove => _events.RemoveHandler(ContextEvents.ContextEnded, value);
        }

        public event Action<Context, object> ChildContextEnding
        {
            add => _events?.AddHandler(ContextEvents.ChildContextEnding, value);
            remove => _events.RemoveHandler(ContextEvents.ChildContextEnding, value);
        }

        public event Action<Context, object> ChildContextEnded
        {
            add => _events?.AddHandler(ContextEvents.ChildContextEnded, value);
            remove => _events.RemoveHandler(ContextEvents.ChildContextEnded, value);
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

		~Context()
		{
			Dispose(false);
			IsDisposed = true;
		}

		protected virtual void Dispose(bool managed)
		{
            End(Disposed);
		}

		#endregion

        private void Raise(object key, Context context, object reason)
        {
            var eventHandler = (Action<Context, object>) _events?[key];
            eventHandler?.Invoke(context, reason);
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
