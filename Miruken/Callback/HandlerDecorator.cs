using System;

namespace Miruken.Callback
{
    public interface IHandlerDecorator
    {
        IHandler Decoratee { get; }
    }

    public abstract class HandlerDecorator 
        : Handler, IHandlerDecorator
    {
        protected HandlerDecorator(IHandler decoratee)
        {
            if (decoratee == null)
                throw new ArgumentNullException(nameof(decoratee));
            Decoratee = decoratee;
        }

        public IHandler Decoratee { get; }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var handled = base.HandleCallback(callback, greedy, composer);
            if (!handled || greedy)
                handled = Decoratee.Handle(callback, greedy, composer) || handled;
            return handled;
        }

        public static IHandler Decorated(IHandler handler, bool deepest)
        {
            var decoratee = handler;
            while (handler != null)
            {
                var decorator = handler as IHandlerDecorator;
                if (decorator == null || (handler = decorator.Decoratee) == null) break;
                if (!deepest) return handler;
                decoratee = handler;
            }
            return decoratee;
        }
    }
}
