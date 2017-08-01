namespace Miruken.Callback
{
    using System;

    public abstract class HandlerDecorator  : Handler, IDecorator
    {
        protected HandlerDecorator(IHandler decoratee)
        {
            if (decoratee == null)
                throw new ArgumentNullException(nameof(decoratee));
            Decoratee = decoratee;
        }

        public IHandler Decoratee { get; }

        object IDecorator.Decoratee => Decoratee;

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            return Decoratee.Handle(callback, ref greedy, composer);
        }
    }
}
