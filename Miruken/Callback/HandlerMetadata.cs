namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;

    public static class HandlerMetadata
    {
        private static readonly ConcurrentDictionary<Type, HandlerDescriptor>
            _descriptors = new ConcurrentDictionary<Type, HandlerDescriptor>();

        public static HandlerDescriptor GetDescriptor(Type type)
        {
            return _descriptors.GetOrAdd(type, t => new HandlerDescriptor(t));
        }

        public static bool Dispatch(
            Type definition, Handler handler, object callback, bool greedy,
            IHandler composer)
        {
            var handled   = false;
            var surrogate = handler.Surrogate;

            if (surrogate != null)
            {
                var descriptor = GetDescriptor(surrogate.GetType());
                handled = descriptor.Dispatch(definition, surrogate, callback, greedy, composer);
            }

            if (!handled || greedy)
            {
                var descriptor = GetDescriptor(handler.GetType());
                handled = descriptor.Dispatch(definition, handler, callback, greedy, composer)
                       || handled;
            }

            return handled;
        }
    }
}
