namespace Miruken.Infrastructure
{
    using System;

    public abstract class DisposableObject : IDisposable
    {
        ~DisposableObject()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public event EventHandler Disposed;
        private void OnDisposed()
        {
            var handler = Disposed;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            OnDisposed();
            GC.SuppressFinalize(this);
        }
    }
}
