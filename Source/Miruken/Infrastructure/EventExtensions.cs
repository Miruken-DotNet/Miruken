namespace Miruken.Infrastructure;

using System;
using System.ComponentModel;

public static class EventExtensions
{
    public static void Raise(this EventHandler handler, object sender)
    {
        var evt = handler;
        evt?.Invoke(sender, EventArgs.Empty);
    }

    public static void Raise<T>(this EventHandler<T> handler, object sender, T args)
        where T : EventArgs
    {
        var evt = handler;
        evt?.Invoke(sender, args);
    }

    public static void Raise(this EventHandlerList events, object sender, object key)
    {
        var eventHandler = (EventHandler)events[key];
        eventHandler?.Invoke(sender, EventArgs.Empty);
    }

    public static void Raise<TEvent>(this EventHandlerList events,
        object sender, object key, TEvent args)
        where TEvent : EventArgs
    {
        var eventHandler = (EventHandler<TEvent>)events[key];
        eventHandler?.Invoke(sender, args);
    }
}