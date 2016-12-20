using System;
using System.Runtime.Remoting.Messaging;
using Miruken.Infrastructure;

namespace Miruken.Callback
{
    public interface IResolving {}

    public partial class Handler
    {
        object IProtocolAdapter.Dispatch(Type protocol, IMethodCallMessage message)
        {
            protocol = protocol ?? message.MethodBase.ReflectedType;

            bool broadcast  = false,
                 duck       = typeof(IDuck).IsAssignableFrom(protocol),
                 useResolve = typeof(IResolving).IsAssignableFrom(protocol);

            var semantics = GetSemantics(this);
            if (semantics != null)
            {
                broadcast  = semantics.HasOption(CallbackOptions.Broadcast);
                duck       = duck || semantics.HasOption(CallbackOptions.Duck);
                useResolve = useResolve || semantics.HasOption(CallbackOptions.Resolve);
            }

            var options = CallbackOptions.None;
            if (duck && semantics?.HasOption(CallbackOptions.Duck) == false)
                options = options | CallbackOptions.Duck;
            if (useResolve && semantics?.HasOption(CallbackOptions.Resolve) == false)
                options = options | CallbackOptions.Resolve;
            var handler = options != CallbackOptions.None
                        ? this.Semantics(options)
                        : this;

            var handleMethod = new HandleMethod(protocol, message, duck);
            var callback     = useResolve
                             ? new ResolveMethod(handleMethod, broadcast)
                             : (object)handleMethod;

            var handled = handler.Handle(callback, broadcast);
            if (!handled && semantics?.HasOption(CallbackOptions.BestEffot) != true)
                throw new MissingMethodException(
                    $"Method '{message.MethodName}' on {message.TypeName} not handled");

            var result = handleMethod.Result;

            return handled || result != null ? result 
                 : RuntimeHelper.GetDefault(handleMethod.ResultType);
        }

        private static CallbackSemantics GetSemantics(IHandler handler)
        {
            var semantics = new CallbackSemantics();
            return handler.Handle(semantics, true) ? semantics : null;
        }

        private bool TryHandleMethod(object callback, bool greedy, IHandler composer)
        {
            var handleMethod = callback as HandleMethod;
            if (handleMethod == null) return false;
            var handled = Surrogate != null && handleMethod.InvokeOn(Surrogate, composer);
            if (!handled || greedy)
                handled = handleMethod.InvokeOn(this, composer) || handled;
            return handled;
        }

        private bool TryResolveMethod(object callback, IHandler composer)
        {
            var resolveMethod = callback as ResolveMethod;
            return resolveMethod != null && resolveMethod.InvokeResolve(this, composer);
        }

        public static IHandler Composer => HandleMethod.Composer;

        public static void Unhandled()
        {
            HandleMethod.Unhandled = true;
        }

        public static Ret Unhandled<Ret>(Ret result = default(Ret))
        {
            HandleMethod.Unhandled = true;
            return result;
        }
    }
}
