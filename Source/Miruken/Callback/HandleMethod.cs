namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Runtime.Remoting.Messaging;
    using Infrastructure;
    using Policy;

    public class HandleMethod : ICallback, IDispatchCallback
    {
        private readonly CallbackSemantics _semantics;

        private static readonly ConcurrentDictionary<MethodInfo, HandleMethodBinding>
            _bindings = new ConcurrentDictionary<MethodInfo, HandleMethodBinding>();

        public HandleMethod(Type protocol, IMethodMessage methodCall, 
            CallbackSemantics semantics = null)
        {
            _semantics = semantics;
            Method     = (MethodInfo)methodCall.MethodBase;
            Arguments  = methodCall.Args;
            Protocol   = protocol ?? Method.ReflectedType;
            ResultType = Method.ReturnType == typeof(void) ? null
                       : Method.ReturnType;
        }

        public Type       Protocol    { get; }
        public MethodInfo Method      { get; }
        public Type       ResultType  { get; }
        public object[]   Arguments   { get; }
        public object     ReturnValue { get; set; }
        public Exception  Exception   { get; set; }

        public object Result
        {
            get { return ReturnValue; }
            set { ReturnValue = value; }
        }

        public CallbackPolicy Policy => null;

        public bool InvokeOn(object target, IHandler composer)
        {
            if (!IsTargetAccepted(target)) return false;

            var method = RuntimeHelper.SelectMethod(Method, target.GetType(), Binding);
            if (method == null) return false;

            var binding = _bindings.GetOrAdd(method, 
                m => new HandleMethodBinding(new MethodDispatch(m)));
            return binding.Dispatch(target, this, composer);
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            var surrogate = (handler as ISurrogate)?.Surrogate;
            var handled   = surrogate != null && InvokeOn(surrogate, composer);
            if (!handled || greedy)
                handled = InvokeOn(handler, composer) || handled;
            return handled;
        }

        public static IHandler RequireComposer()
        {
            var composer = Composer;
            if (composer == null)
                throw new InvalidOperationException(
                    "Composer not available.  Did you call this method directly?");
            return composer;
        }

        private bool IsTargetAccepted(object target)
        {
            return _semantics.HasOption(CallbackOptions.Strict)
                 ? Protocol.IsTopLevelInterface(target.GetType())
                 : _semantics.HasOption(CallbackOptions.Duck)
                || Protocol.IsInstanceOfType(target);
        }

        public static IHandler Composer  => HandleMethodBinding.Composer;
        public static bool     Unhandled => HandleMethodBinding.Unhandled;

        private const BindingFlags Binding = BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.NonPublic;
    }
}
