namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;

    public class Batch : IDispatchCallback
    {
        private readonly bool _all;
        private readonly List<Operation> _operations;
        private List<Promise> _promises;

        private class Operation
        {
            public Action<IHandler> Action;
            public bool             Handled;
        }

        public Batch(bool all = true)
        {
            _all        = all;
            _operations = new List<Operation>();
        }

        public CallbackPolicy Policy => null;

        public Promise Complete()
        {
            return _promises != null
                ? Promise.All(_promises.ToArray())
                : Promise.Empty;
        }

        public Batch Add(Action<IHandler> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            _operations.Add(new Operation {Action = action});
            return this;
        }

        public Batch Add(Func<IHandler, object> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return Add(handler => { action(handler); });
        }

        public Batch Add(Func<IHandler, Promise> action)
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
            });
        }

        public Batch Add(Func<IHandler, Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return Add(handler =>
            {
                var task = action(handler);
                if (task != null && (task.Status != TaskStatus.Faulted ||
                    !(task.Exception?.InnerException is InterruptBatchException)))
                {
                    (_promises ?? (_promises = new List<Promise>()))
                        .Add(task);
                }
            });
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
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
            private bool _handled;

            public ProxyHandler(object handler, IHandler composer)
                : base(handler)
            {
                _composer = composer;
            }

            public bool Dispatch(Operation operation)
            {
                try
                {
                    _handled = true;
                    operation.Action(this);
                    return _handled;
                }
                catch (InterruptBatchException)
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
                {
                    _handled = false;  // async delegates
                    throw new InterruptBatchException();
                }
                return true;
            }
        }

        private class InterruptBatchException : CancelledException { }
    }
}
