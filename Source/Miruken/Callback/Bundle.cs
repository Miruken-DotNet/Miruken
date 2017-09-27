namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;


    public class Bundle : IAsyncCallback, 
        IResolveCallback, IDispatchCallback
    {
        private readonly bool _all;
        private List<Operation> _operations;
        private List<Promise> _promises;
        private bool _resolving;

        public delegate bool NotifyDelegate(bool handled);

        private class Operation
        {
            public Action<IHandler> Action;
            public NotifyDelegate   Notify;
            public bool             Handled;
        }

        public Bundle(bool all = true)
        {
            _all = all;
        }

        public bool IsEmpty => _operations == null;
        public bool WantsAsync { get; set; }
        public bool IsAsync => _promises != null;
        public CallbackPolicy Policy => null;

        public Promise Complete()
        {
            if (_operations == null)
                return Promise.Empty;
            if  (_all || !_operations.Any(op => op.Handled))
                _operations.ForEach(op =>
                {
                    if (!op.Handled)
                        op.Notify?.Invoke(false);
                });
            return IsAsync
                ? Promise.All(_promises.ToArray())
                    .Then((r,s) => Promise.Empty)
                : Promise.Empty;
        }

        public Bundle Add(Action<IHandler> action, NotifyDelegate notify = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (WantsAsync)
            {
                var act = action;
                action = handler =>
                {
                    try
                    {
                        act(handler);
                    }
                    catch (Exception ex) when(!(ex is RejectedException))
                    {
                        (_promises ?? (_promises = new List<Promise>()))
                            .Add(Promise.Rejected(ex));
                    }
                };
            }
            (_operations ?? (_operations = new List<Operation>()))
                .Add(new Operation
                {
                    Action = action,
                    Notify = notify
                });
            return this;
        }

        public Bundle Add(Func<IHandler, object> action, NotifyDelegate notify = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return Add(handler => { action(handler); }, notify);
        }

        public Bundle Add(Func<IHandler, Promise> action, NotifyDelegate notify = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return Add(handler =>
            {
                var promise = action(handler);
                if (promise != null)
                    (_promises ?? (_promises = new List<Promise>()))
                        .Add(promise);
            }, notify);
        }

        public Bundle Add(Func<IHandler, Task> action, NotifyDelegate notify = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return Add(handler =>
            {
                var task = action(handler);
                if (task != null)
                {
                    if (task.Status == TaskStatus.Faulted)
                    {
                        var rejected = task.Exception?.InnerException as RejectedException;
                        if (rejected != null)
                            ExceptionDispatchInfo.Capture(rejected).Throw();
                    }
                    (_promises ?? (_promises = new List<Promise>()))
                        .Add(task);
                }
            }, notify);
        }

        object IResolveCallback.GetResolveCallback()
        {
            return _resolving ? this : new Bundle(_all)
            {
                _operations = _operations == null ? null
                            : new List<Operation>(_operations),
                _resolving  = true
            };
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            if (_operations == null) return false;

            IHandler proxy = new ProxyHandler(handler, composer);
            if (_resolving) proxy = proxy.Resolve();

            var handled = _all;
            foreach (var operation in _operations)
            {
                if (_all || greedy)
                {
                    var stop = false;
                    var opHandled = operation.Handled;
                    if (!opHandled || greedy)
                    {
                        var dispatched = Dispatch(proxy, operation);
                        opHandled = operation.Handled = dispatched || opHandled;
                        if (dispatched)
                            stop = operation.Notify?.Invoke(true) ?? false;
                    }
                    handled = _all ? opHandled && handled
                            : opHandled || handled;
                    if (stop) break;
                }
                else
                {
                    var opHandled = Dispatch(proxy, operation);
                    operation.Handled |= opHandled;
                    if (opHandled)
                    {
                        operation.Notify?.Invoke(true);
                        return true;
                    }
                }
            }
            return handled;
        }

        private static bool Dispatch(IHandler proxy, Operation operation)
        {
            try
            {
                operation.Action(proxy);
                return true;
            }
            catch (RejectedException)
            {
                return false;
            }
        }

        private class ProxyHandler : HandlerAdapter
        {
            private readonly IHandler _composer;

            public ProxyHandler(object handler, IHandler composer)
                : base(handler)
            {
                _composer = composer;
            }

            protected override bool HandleCallback(
                object callback, ref bool greedy, IHandler composer)
            {
                if (callback is Composition) return false;
                composer = new CascadeHandler(composer, _composer);
                if (!base.HandleCallback(callback, ref greedy, composer))
                    throw new RejectedException(callback);
                return true;
            }
        }
    }
}
