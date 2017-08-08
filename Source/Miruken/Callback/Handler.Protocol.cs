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

            if (!semantics.IsSpecified(CallbackOptions.Duck) && protocol.Is<IDuck>())
                options |= CallbackOptions.Duck;

            if (!semantics.IsSpecified(CallbackOptions.Strict) && protocol.Is<IStrict>())
                options |= CallbackOptions.Strict;

            if (protocol.Is<IResolving>())
            {
                if (semantics.IsSpecified(CallbackOptions.Broadcast))
                    options |= CallbackOptions.Broadcast;
                handler = handler.Resolve();
            }

            if (options != CallbackOptions.None)
            {
                semantics.SetOption(options, true);
                handler = handler.Semantics(options);
            }

            var handleMethod = new HandleMethod(protocol, message, semantics);
            if (!handler.Handle(handleMethod))
                throw new MissingMethodException(
                    $"Method '{message.MethodName}' on {message.TypeName} not handled");

            return handleMethod.Result 
                ?? RuntimeHelper.GetDefault(handleMethod.ResultType);
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
