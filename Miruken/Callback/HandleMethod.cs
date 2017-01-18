namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting.Messaging;
    using Concurrency;
    using Infrastructure;

    public class HandleMethod : ICallback
    {
        private readonly MethodInfo _method;
        private readonly object[] _args;
        private readonly CallbackSemantics _semantics;

        public HandleMethod(Type protocol, IMethodMessage methodCall, 
            CallbackSemantics semantics = null)
        {
            _semantics = semantics;
            _method    = (MethodInfo)methodCall.MethodBase;
            _args      = methodCall.Args;
            Protocol   = protocol ?? _method.ReflectedType;
            ResultType = _method.ReturnType == typeof(void) ? null
                       : _method.ReturnType;
        }

        public Type ResultType { get; }

        public object Result
        {
            get { return ReturnValue; }
            set { ReturnValue = value; }
        }

        public object ReturnValue { get; set; }

        public Exception Exception { get; set; }

        public Type Protocol { get; }

        public bool InvokeOn(object target, IHandler composer)
        {
            if (!IsAcceptableTarget(target)) return false;

            var targetMethod = RuntimeHelper.SelectMethod(_method, target.GetType(), Binding);
            if (targetMethod == null) return false;

            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;

            try
            {
                Composer    = composer;
                Unhandled   = false;
                ReturnValue = targetMethod.Invoke(target, Binding, null, _args, null);
                return !Unhandled;
            }
            catch (Exception exception)
            {
                var tie = exception as TargetException;
                if (tie != null) exception = tie.InnerException;
                Exception = exception;
                if (!typeof(Promise).IsAssignableFrom(ResultType)) throw;
                ReturnValue = Promise.Rejected(exception).Coerce(ResultType);
                return true;
            }
            finally
            {
                Unhandled = oldUnhandled;
                Composer  = oldComposer;
            }
        }

        private bool IsAcceptableTarget(object target)
        {
            return _semantics.HasOption(CallbackOptions.Strict)
                 ? RuntimeHelper.IsTopLevelInterface(Protocol, target.GetType())
                 : _semantics.HasOption(CallbackOptions.Duck) 
                || Protocol.IsInstanceOfType(target);
        }

        public static IHandler RequireComposer()
        {
            var composer = Composer;
            if (composer == null)
                throw new InvalidOperationException(
                    "Composer not available.  Did you call this method directly?");
            return composer;
        }

        [ThreadStatic] public static IHandler Composer;
        [ThreadStatic] public static bool     Unhandled;

        private const BindingFlags Binding = BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.NonPublic;
    }
}
