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
            var handled = base.HandleCallback(callback, ref greedy, composer);
            if (!handled || greedy)
                handled = Decoratee.Handle(callback, ref greedy, composer) || handled;
            return handled;
        }
    }
}
