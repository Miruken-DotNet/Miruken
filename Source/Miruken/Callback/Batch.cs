namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;

    public class Batch : IAsyncCallback, IDispatchCallback
    {
        private readonly bool _all;
        private List<Operation> _operations;
        private List<Promise> _promises;

        private class Operation
        {
            public Action<IHandler> Action;
            public Action           Unhandled;
            public bool             Handled;
        }

        public Batch(bool all = true)
        {
            _all = all;
        }

        public bool           IsEmpty => _operations == null;
        public bool           WantsAsync { get; set; }
        public bool           IsAsync => _promises != null;
        public CallbackPolicy Policy => null;

        public Promise Complete()
        {
            if (_operations == null)
                return Promise.Empty;
            if  (_all || !_operations.Any(op => op.Handled))
                _operations.ForEach(op =>
                {
                    if (!op.Handled)
                        op.Unhandled?.Invoke();
                });
            return IsAsync
                ? Promise.All(_promises.ToArray())
                    .Then((r,s) => Promise.Empty)
                : Promise.Empty;
        }

        public Batch Add(Action<IHandler> action, Action unhandled = null)
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
                    Action    = action,
                    Unhandled = unhandled
                });
            return this;
        }

        public Batch Add(Func<IHandler, object> action, Action unhandled = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return Add(handler => { action(handler); }, unhandled);
        }

        public Batch Add(Func<IHandler, Promise> action, Action unhandled = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return Add(handler =>
            {
                var promise = action(handler);
                if (promise != null)
                {
                    (_promises ?? (_promises = new List<Promise>()))
                        .Add(promise);
                }
            }, unhandled);
        }

        public Batch Add(Func<IHandler, Task> action, Action unhandled = null)
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
            }, unhandled);
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            if (_operations == null) return true;
            var isGreedy = greedy;
            var proxy    = new ProxyHandler(handler, composer);
            return _all ? _operations.Aggregate(true, (result, op) =>
            {
                var handled = op.Handled;
                if (!handled || isGreedy)
                    handled = op.Handled = proxy.Dispatch(op) || handled;
                return handled && result;
            }) : _operations.Any(op =>
            {
                var handled = proxy.Dispatch(op);
                op.Handled |= handled;
                return handled;
            });
        }

        private class ProxyHandler : HandlerAdapter
        {
            private readonly IHandler _composer;

            public ProxyHandler(object handler, IHandler composer)
                : base(handler)
            {
                _composer = composer;
            }

            public bool Dispatch(Operation operation)
            {
                try
                {
                    operation.Action(this);
                    return true;
                }
                catch (RejectedException)
                {
                    return false;
                }
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
