﻿namespace Miruken.Context
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Threading;
	using Callback;
	using Graph;
	using Microsoft.Extensions.DependencyInjection;

	public enum ContextState
    {
        Active = 0,
        Ending,
        Ended
    }

    public class Context : CompositeHandler,
        IHandlerAxis, ITraversing, IServiceScope, IServiceScopeFactory
    {
	    private EventHandlerList _events;
	    private ReaderWriterLockSlim _lock;
	    private readonly List<Context> _children;

        public static readonly object AlreadyEnded = new();
	    public static readonly object Unwinded     = new();
	    public static readonly object Disposed     = new();

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
	            Raise(ChildContextEndingEvent, ctx, reason);
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
	            Raise(ChildContextEndedEvent, ctx, reason);
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
	        return new(this);
	    }
	    
	    IServiceProvider IServiceScope.ServiceProvider => this;

	    IServiceScope IServiceScopeFactory.CreateScope() => CreateChild();

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
	            handled |= parent.Handle(callback, ref greedy, composer);
	        return handled;
	    }

	    public bool Handle(TraversingAxis axis, object callback,
	        ref bool greedy, IHandler composer)
	    {
	        composer ??= new CompositionScope(this);

            if (axis == TraversingAxis.Self)
	            return base.HandleCallback(callback, ref greedy, composer);

	        var g = greedy;
	        var handled = false;
            Traverse(axis, node =>
            {
                handled |= (node == this
                    ? BaseHandle(callback, ref g, composer)
                    : ((Context)node).Handle(
                        TraversingAxis.Self, callback, ref g, composer));
                return handled && !g;
            });
	        greedy = g;
	        return handled;
	    }

        private bool BaseHandle(object callback, ref bool greedy, IHandler composer) =>
	        base.HandleCallback(callback, ref greedy, composer);

        public void Traverse(TraversingAxis axis, Visitor visitor) =>
	        TraversingHelper.Traverse(this, axis, visitor);

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
            Raise(ContextEndingEvent, this, reason);

            try
            {
                Unwind();
                InternalEnd(reason);
            }
            finally
            {
                State = ContextState.Ended;
                Raise(ContextEndedEvent, this, reason);
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
                        _events.AddHandler(ContextEndingEvent, value);
                        break;
                    case ContextState.Ending:
                        value(this, AlreadyEnded);
                        break;
                }
            }
            remove => _events.RemoveHandler(ContextEndingEvent, value);
        }
	    private static readonly object ContextEndingEvent = new();

        public event Action<Context, object> ContextEnded
        {
            add
            {
                switch (State)
                {
                    case ContextState.Active:
                        _events.AddHandler(ContextEndedEvent, value);
                        break;
                    case ContextState.Ended:
                        value(this, AlreadyEnded);
                        break;
                }
            }
            remove => _events.RemoveHandler(ContextEndedEvent, value);
        }
	    private static readonly object ContextEndedEvent = new();

        public event Action<Context, object> ChildContextEnding
        {
            add => _events?.AddHandler(ChildContextEndingEvent, value);
            remove => _events.RemoveHandler(ChildContextEndingEvent, value);
        }
	    private static readonly object ChildContextEndingEvent = new();

        public event Action<Context, object> ChildContextEnded
        {
            add => _events?.AddHandler(ChildContextEndedEvent, value);
            remove => _events.RemoveHandler(ChildContextEndedEvent, value);
        }
	    private static readonly object ChildContextEndedEvent = new();

	    private void Raise(object key, Context context, object reason)
	    {
	        var eventHandler = (Action<Context, object>)_events?[key];
	        eventHandler?.Invoke(context, reason);
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

        private void AssertActive()
        {
            if (State != ContextState.Active)
                throw new Exception("The context has already ended");
        }
    }
}
