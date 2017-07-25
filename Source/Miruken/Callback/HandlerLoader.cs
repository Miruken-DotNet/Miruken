namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using Policy;

    public class HandlerLoader : IDisposable
    {
        private readonly BlockingCollection<Type> _queue;

        public HandlerLoader()
        {
            _queue = new BlockingCollection<Type>();

            new Thread(CreateDescriptors)
            {
                Name         = "Miruken Handler Loader",
                Priority     = ThreadPriority.BelowNormal,
                IsBackground = true
            }.Start();
        }

        public void LoadHandler(Type handlerType)
        {
            _queue.Add(handlerType);    
        }

        private void CreateDescriptors()
        {
            while (!_queue.IsCompleted)
            {
                try
                {
                    HandlerDescriptor.GetDescriptor(_queue.Take());
                }
                catch (InvalidOperationException)
                {
                    // CompleteAdding called after check
                }
            }
            _queue.Dispose();
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
        }
    }
}
