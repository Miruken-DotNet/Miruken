namespace Miruken.Infrastructure
{
    using System;

    public class DisposableAction<T> : IDisposable
    {
        readonly Action<T> _action;
        readonly T _val;

        public DisposableAction(Action<T> action, T val)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            _action = action;
            _val    = val;
        }

        public T Value
        {
            get { return _val; }
        }

        public void Dispose()
        {
            _action(_val);
        }
    }

    public class DisposableAction : IDisposable
    {
        readonly Action _action;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            if (_action != null)
                _action();
        }
    }
}
