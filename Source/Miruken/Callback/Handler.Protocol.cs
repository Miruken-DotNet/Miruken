namespace Miruken.Callback
{
    using System;
    using System.Runtime.Remoting.Messaging;
    using Infrastructure;
    using Policy;

    public interface IResolving {}

    public partial class Handler
    {
        object IProtocolAdapter.Dispatch(Type protocol, IMethodCallMessage message)
        {
            IHandler handler = this;
            protocol = protocol ?? message.MethodBase.ReflectedType;

            var options   = CallbackOptions.None;
            var semantics = new CallbackSemantics();
            handler.Handle(semantics, true);

            if (!semantics.IsSpecified(CallbackOptions.Duck) &&
                typeof(IDuck).IsAssignableFrom(protocol))
                options |= CallbackOptions.Duck;

            if (!semantics.IsSpecified(CallbackOptions.Strict) &&
                typeof(IStrict).IsAssignableFrom(protocol))
                options |= CallbackOptions.Strict;

            if (!semantics.IsSpecified(CallbackOptions.Resolve) &&
                typeof(IResolving).IsAssignableFrom(protocol))
                options |= CallbackOptions.Resolve;

            if (options != CallbackOptions.None)
            {
                semantics.SetOption(options, true);
                handler = this.Semantics(options);
            }

            var broadcast    = semantics.HasOption(CallbackOptions.Broadcast);
            var handleMethod = new HandleMethod(protocol, message, semantics);
            var callback     = semantics.HasOption(CallbackOptions.Resolve)
                             ? new Resolve(protocol, broadcast, handleMethod)
                             : (object)handleMethod;

            var handled = handler.Handle(callback, broadcast);
            if (!(handled || semantics.HasOption(CallbackOptions.BestEffot)))
                throw new MissingMethodException(
                    $"Method '{message.MethodName}' on {message.TypeName} not handled");

            var result = handleMethod.Result;

            return handled || result != null ? result 
                 : RuntimeHelper.GetDefault(handleMethod.ResultType);
        }

        public static IHandler Composer => HandleMethodBinding.Composer;

        public static void Unhandled()
        {
            HandleMethodBinding.Unhandled = true;
        }

        public static Ret Unhandled<Ret>(Ret result = default(Ret))
        {
            HandleMethodBinding.Unhandled = true;
            return result;
        }
    }
}
