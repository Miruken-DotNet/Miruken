using System;

namespace SixFlags.CF.Miruken.Callback
{
    public interface IResolving {}

    public partial class CallbackHandler
    {
        #region Protocol

        void IProtocolAdapter.Do<T>(Action<T> action)
        {
            this.Do(action); 
        }

        R IProtocolAdapter.Do<T, R>(Func<T, R> func)
        {
            return this.Do(func);
        }

        #endregion

        private bool TryHandleMethod(object callback, bool greedy, ICallbackHandler composer)
        {
            var handleMethod = callback as HandleMethod;
            if (handleMethod == null) return false;
            var handled = _surrogate != null && handleMethod.InvokeOn(_surrogate, composer);
            if (!handled || greedy)
                handled = handleMethod.InvokeOn(this, composer) || handled;
            return handled;
        }

        private static bool TryResolveMethod(object callback, ICallbackHandler composer)
        {
            var resolveMethod = callback as ResolveMethod;
            return resolveMethod != null && resolveMethod.InvokeResolve(composer);
        }

        public static ICallbackHandler Composer
        {
            get { return HandleMethod.Composer; }
        }

        public static void Unhandled()
        {
            HandleMethod.Unhandled = true;
        }

        public static Ret Unhandled<Ret>()
        {
            HandleMethod.Unhandled = true;
            return default (Ret);
        }
    }

    public static class CallbackHandleMethodExtensions
    {
        public static void Do<T>(this ICallbackHandler handler, Action<T> action)
            where T : class
        {
            if (handler == null) return;
            var handleMethod = new HandleAction<T>(action);
            Do(handleMethod, handler, typeof(T));
        }

        public static R Do<T, R>(this ICallbackHandler handler, Func<T, R> func)
            where T : class
        {
            if (handler == null) return default(R);
            var handleMethod = new HandleFunc<T, R>(func);
            return Do(handleMethod, handler, typeof (T))
                 ? (R) handleMethod.ReturnValue
                 : default(R);
        }

        private static bool Do(HandleMethod method, ICallbackHandler handler, Type protocol)
        {
            bool broadcast  = false,
                 useResolve = typeof(IResolving).IsAssignableFrom(protocol);

            var semantics  = GetSemantics(handler);
            if (semantics != null)
            {
                broadcast  = semantics.HasOption(CallbackOptions.Broadcast);
                useResolve = useResolve || semantics.HasOption(CallbackOptions.Resolve);
            }

            var callback = useResolve
                         ? new ResolveMethod(method, broadcast)
                         : (object)method;

            var handled = handler.Handle(callback, broadcast && !useResolve);
            if (!handled && (semantics == null || 
                !semantics.HasOption(CallbackOptions.BestEffot)))
                throw new MissingMethodException();

            return handled;
        }

        private static CallbackSemantics GetSemantics(ICallbackHandler handler)
        {
            var semantics = new CallbackSemantics();
            return handler.Handle(semantics, true) ? semantics : null;
        }
    }
}
