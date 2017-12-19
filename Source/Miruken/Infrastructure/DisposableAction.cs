namespace Miruken.Infrastructure
{
    using System;

    public class DisposableAction<T> : IDisposable
    {
        private readonly Action<T> _action;
        private readonly T _val;

        public DisposableAction(Action<T> action, T val)
        {
            _action = action
                   ?? throw new ArgumentNullException(nameof(action));
            _val    = val;
        }

        public T Value => _val;

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
            _action?.Invoke();
        }
    }
}
