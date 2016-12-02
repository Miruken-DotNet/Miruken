namespace Miruken.Infrastructure
{
    using System;

    public abstract class DisposableObject:IDisposable
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
            if (handler != null)
                Disposed(this, EventArgs.Empty);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            OnDisposed();
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public static class DisposableExtensions
    {
        public static Exception TryDispose(this IDisposable obj)
        {
            Exception r = null;
            if (obj != null)
            {
                try
                {
                    obj.Dispose();
                }
                catch (Exception e)
                {
                    r = e;
                }
            }
            return r;
        }
    }
}
