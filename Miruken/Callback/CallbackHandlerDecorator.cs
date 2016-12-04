using System;

namespace SixFlags.CF.Miruken.Callback
{
    public interface ICallbackHandlerDecorator
    {
        ICallbackHandler Decoratee { get; }
    }

    public abstract class CallbackHandlerDecorator 
        : CallbackHandler, ICallbackHandlerDecorator
    {
        protected CallbackHandlerDecorator(ICallbackHandler decoratee)
        {
            if (decoratee == null)
                throw new ArgumentNullException("decoratee");
            Decoratee = decoratee;
        }

        public ICallbackHandler Decoratee { get; private set; }

        protected override bool HandleCallback(
            object callback, bool greedy, ICallbackHandler composer)
        {
            var handled = base.HandleCallback(callback, greedy, composer);
            if (!handled || greedy)
                handled = Decoratee.Handle(callback, greedy, composer) || handled;
            return handled;
        }

        public static ICallbackHandler Decorated(ICallbackHandler handler, bool deepest)
        {
            var decoratee = handler;
            while (handler != null)
            {
                var decorator = handler as ICallbackHandlerDecorator;
                if (decorator == null || (handler = decorator.Decoratee) == null) break;
                if (!deepest) return handler;
                decoratee = handler;
            }
            return decoratee;
        }
    }
}
