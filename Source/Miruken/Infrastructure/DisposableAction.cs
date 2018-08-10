namespace Miruken.Infrastructure
{
    using System;

    public class DisposableAction<T> : IDisposable
    {
        private readonly Action<T> _action;

        public DisposableAction(Action<T> action, T val)
        {
            _action = action
                   ?? throw new ArgumentNullException(nameof(action));
            Value   = val;
        }

        public T Value { get; }

        public void Dispose()
        {
            _action(Value);
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
