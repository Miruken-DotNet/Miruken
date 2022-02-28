using System;

namespace Miruken.Callback;

public delegate bool HandlerFilterDelegate(
    object callback, IHandler composer, Func<bool> proceed
);

public class FilteredHandler : DecoratedHandler
{
    private readonly HandlerFilterDelegate _filter;
    private readonly bool _reentrant;

    public FilteredHandler(
        IHandler handler, HandlerFilterDelegate filter, 
        bool reentrant = false) : base(handler)
    {
        _filter    = filter ?? throw new ArgumentNullException(nameof(filter));
        _reentrant = reentrant;
    }

    protected override bool HandleCallback(
        object callback, ref bool greedy, IHandler composer)
    {
        if (!_reentrant && callback is Composition) {                                                                                              
            return base.HandleCallback(callback, ref greedy, composer);                                                                                                   
        }
        var g = greedy;
        var handled = _filter(callback, composer,
            () => base.HandleCallback(callback, ref g, composer));
        greedy = g;
        return handled;
    }
}