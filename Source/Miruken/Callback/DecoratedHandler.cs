namespace Miruken.Callback;

using System;

public abstract class DecoratedHandler  : Handler, IDecorator
{
    protected DecoratedHandler(IHandler decoratee)
    {
        Decoratee = decoratee
                    ?? throw new ArgumentNullException(nameof(decoratee));
    }

    public IHandler Decoratee { get; }

    object IDecorator.Decoratee => Decoratee;

    protected override bool HandleCallback(
        object callback, ref bool greedy, IHandler composer)
    {
        return Decoratee.Handle(callback, ref greedy, composer)
               || base.HandleCallback(callback, ref greedy, composer);
    }
}