using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Miruken.Concurrency;

namespace Miruken.Callback
{
    public class HandleMethod : ICallback
    {
        private readonly MethodInfo _method;
        private readonly Type[] _parameters;
        private readonly object[] _args;

        public HandleMethod(IMethodMessage methodCall)
        {
           _method      = (MethodInfo)methodCall.MethodBase;
            _parameters = _method.GetParameters().Select(p => p.ParameterType).ToArray();
            _args       = methodCall.Args;
            TargetType  = _method.ReflectedType;
            ResultType  = _method.ReturnType == typeof(void) ? null
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

        public Type TargetType { get; }

        public bool InvokeOn(object target, IHandler composer)
        {
            var targetMethod = MatchMethod(target);
            if (targetMethod == null) return false;

            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;

            try
            {
                Composer  = composer;
                Unhandled = false;
                var returnValue = targetMethod.Invoke(target, Binding, null, _args, null);
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

        private MethodInfo MatchMethod(object target)
        {
            MethodInfo[] candidates = null;

            var targetType = target.GetType();
            if (TargetType.IsInterface && TargetType.IsInstanceOfType(target))
            {
                var mapping = targetType.GetInterfaceMap(TargetType);
                candidates = mapping.TargetMethods;
            }

            candidates = candidates ?? targetType.GetMethods(Binding);

            return candidates.FirstOrDefault(m => m.Name == _method.Name &&
                    m.GetParameters().Select(p => p.ParameterType)
                    .SequenceEqual(_parameters));
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
