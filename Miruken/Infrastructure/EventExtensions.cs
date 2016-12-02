namespace Miruken.Infrastructure
{
    using System;
    using System.ComponentModel;

    public static class EventExtensions
    {
        public static void Raise(this EventHandler handler, object sender)
        {
            var evt = handler;
            if (evt != null) evt(sender, EventArgs.Empty);
        }

        public static void Raise<T>(this EventHandler<T> handler, object sender, T args)
            where T : EventArgs
        {
            var evt = handler;
            if (evt != null) evt(sender, args);
        }

        public static void Raise(this EventHandlerList events, object sender, object key)
        {
            var eventHandler = (EventHandler)events[key];
            if (eventHandler != null)
                eventHandler(sender, EventArgs.Empty);
        }

        public static void Raise<EventArgsT>(this EventHandlerList events, object sender, object key, EventArgsT args)
            where EventArgsT : EventArgs
        {
            var eventHandler = (EventHandler<EventArgsT>)events[key];
            if (eventHandler != null)
                eventHandler(sender, args);
        }
    }
}
