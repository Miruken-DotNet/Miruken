using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Miruken.Concurrency;

namespace Miruken.Callback
{
    public class HandleMethod : ICallback
    {
        private readonly IMethodCallMessage _methodCall;

        public HandleMethod(IMethodCallMessage methodCall)
        {
            _methodCall = methodCall;
            var method  = (MethodInfo)methodCall.MethodBase;
            TargetType  = method.ReflectedType;
            ResultType  = method.ReturnType == typeof(void) ? null
                        : method.ReturnType;
        }

        public Type ResultType { get; }

        public object Result
        {
            get { return ReturnValue; }
            set { ReturnValue = value; }
        }

        public object ReturnValue { get; set; }

        public Exception Exception { get; set; }

        public Type TargetType { get; }

        public bool InvokeOn(object target, IHandler composer)
        {
            if (!TargetType.IsInstanceOfType(target))
                return false;

            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;

            try
            {
                Composer  = composer;
                Unhandled = false;
                var returnValue = InvokeMethod(target);
                if (Unhandled) return false;
                ReturnValue = returnValue;
                return true;
            }
            catch (Exception exception)
            {
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

        private object InvokeMethod(object target)
        {
            var method = _methodCall.MethodBase;
            var targetMethod = target.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == method.Name &&
                    m.GetParameters().Select(p => p.ParameterType).SequenceEqual(
                        method.GetParameters().Select(p => p.ParameterType)));
            return targetMethod?.Invoke(target, _methodCall.Args);
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
    }
}
