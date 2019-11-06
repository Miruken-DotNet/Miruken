namespace Miruken.Callback
{
    using System;
    using Infrastructure;
    using Policy.Bindings;

    public interface IResolving {}

    public partial class Handler
    {
        object IProtocolAdapter.Dispatch(HandleMethod handleMethod)
        {
            IHandler handler = this;
            var options   = CallbackOptions.None;
            var semantics = new CallbackSemantics();
            handler.Handle(semantics, true);

            var protocol = handleMethod.Protocol;
            if (!semantics.IsSpecified(CallbackOptions.Duck) && protocol.Is<IDuck>())
                options |= CallbackOptions.Duck;

            if (!semantics.IsSpecified(CallbackOptions.Strict) && protocol.Is<IStrict>())
                options |= CallbackOptions.Strict;

            if (protocol.Is<IResolving>())
            {
                if (semantics.IsSpecified(CallbackOptions.Broadcast))
                    options |= CallbackOptions.Broadcast;
            }

            if (options != CallbackOptions.None)
            {
                semantics.SetOption(options, true);
                handler = handler.Semantics(options);
            }

            handleMethod.Semantics = semantics;
            if (!handler.Handle(handleMethod))
                throw new MissingMethodException(
                    $"Method '{handleMethod.Method.Name}' on {protocol.FullName} not handled");

            return handleMethod.Result 
                ?? RuntimeHelper.GetDefault(handleMethod.ResultType);
        }

        public static IHandler Composer => HandleMethodBinding.Composer;

        public static void Unhandled()
        {
            HandleMethodBinding.Unhandled = true;
        }

        public static TRet Unhandled<TRet>(TRet result = default)
        {
            HandleMethodBinding.Unhandled = true;
            return result;
        }
    }
}
