namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Concurrency;
    using Policy;

    public class IncompleteBatchException : Exception { }

    public class Batch : IDispatchCallback
    {
        private readonly bool _all;
        private readonly List<Operation> _operations;
        private List<Promise> _promises;

        private class Operation
        {
            public Action<IHandler> Op;
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
            var complete = _all
                ? _operations.All(op => op.Handled)
                : _operations.Any(op => op.Handled);
            if (!complete)
                return Promise.Rejected(new IncompleteBatchException());
            return _promises != null
                ? Promise.All(_promises.ToArray())
                : Promise.Empty;

        }

        public Batch Add(Action<IHandler> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            _operations.Add(new Operation {Op = operation});
            return this;
        }

        public Batch Add(Func<IHandler, object> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            return Add(handler => { operation(handler); });
        }

        public Batch Add(Func<IHandler, Promise> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            return Add(handler =>
            {
                var promise = operation(handler);
                if (promise != null)
                {
                    (_promises ?? (_promises = new List<Promise>()))
                        .Add(promise);
                }
            });
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            var isGreedy = greedy;
            var proxy    = new ProxyHandler(handler);
            return _all ? _operations.Aggregate(true, (result, op) =>
            {
                var handled = op.Handled;
                if (!handled || isGreedy)
                    handled = op.Handled = proxy.Dispatch(op.Op) || handled;
                return handled && result;
            }) : _operations.Any(op =>
            {
                var handled = proxy.Dispatch(op.Op);
                op.Handled |= handled;
                return handled;
            });
        }

        private class ProxyHandler : HandlerAdapter
        {
            public ProxyHandler(object handler)
                : base(handler)
            {
            }

            public bool Dispatch(Action<IHandler> action)
            {
                try
                {
                    action(this);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            protected override bool HandleCallback(
                object callback, ref bool greedy, IHandler composer)
            {
                var handled = base.HandleCallback(callback, ref greedy, composer);
                if (!(handled || callback is Composition))
                    throw new InterruptBatchException();
                return handled;
            }
        }

        private class InterruptBatchException : Exception { }
    }
}
