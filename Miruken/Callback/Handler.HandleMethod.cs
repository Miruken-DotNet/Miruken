using System;
using System.Runtime.Remoting.Messaging;
using Miruken.Infrastructure;

namespace Miruken.Callback
{
    public interface IResolving {}

    public partial class Handler
    {
        object IProtocolAdapter.Dispatch(IMethodCallMessage message)
        {
            var protocol = message.MethodBase.ReflectedType;

            bool broadcast  = false,
                 strict     = typeof(IStrict).IsAssignableFrom(protocol),
                 useResolve = typeof(IResolving).IsAssignableFrom(protocol);

            var semantics = GetSemantics(this);
            if (semantics != null)
            {
                broadcast  = semantics.HasOption(CallbackOptions.Broadcast);
                strict     = strict || semantics.HasOption(CallbackOptions.Strict);
                useResolve = useResolve || semantics.HasOption(CallbackOptions.Resolve);
            }

            var options = CallbackOptions.None;
            if (strict && semantics?.HasOption(CallbackOptions.Strict) == false)
                options = options | CallbackOptions.Strict;
            if (useResolve && semantics?.HasOption(CallbackOptions.Resolve) == false)
                options = options | CallbackOptions.Resolve;
            var handler = options != CallbackOptions.None
                        ? this.Semantics(options)
                        : this;

            var handleMethod = new HandleMethod(message, strict);
            var callback     = useResolve
                             ? new ResolveMethod(handleMethod, broadcast)
                             : (object)handleMethod;

            var handled = handler.Handle(callback, broadcast && !useResolve);
            if (!handled && semantics?.HasOption(CallbackOptions.BestEffot) != true)
                throw new MissingMethodException(
                    $"Method '{message.MethodName}' on {message.TypeName} not handled");

            return handled ? handleMethod.Result 
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

        private static bool TryResolveMethod(object callback, IHandler composer)
        {
            var resolveMethod = callback as ResolveMethod;
            return resolveMethod != null && resolveMethod.InvokeResolve(composer);
        }

        public static IHandler Composer => HandleMethod.Composer;

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
}
