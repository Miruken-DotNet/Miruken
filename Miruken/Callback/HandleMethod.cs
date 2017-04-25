namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting.Messaging;
    using Concurrency;
    using Infrastructure;

    public class HandleMethod : ICallback, ICallbackDispatch
    {
        private readonly CallbackSemantics _semantics;

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

        public Type Protocol { get; }

        public MethodInfo Method { get; }

        public Type ResultType { get; }

        public object[] Arguments { get; }

        public object Result
        {
            get { return ReturnValue; }
            set { ReturnValue = value; }
        }

        public object ReturnValue { get; set; }

        public Exception Exception { get; set; }

        public bool InvokeOn(object target, IHandler composer)
        {
            if (!IsTargetAccepted(target)) return false;

            var targetMethod = RuntimeHelper.SelectMethod(Method, target.GetType(), Binding);
            if (targetMethod == null) return false;

            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;

            try
            {
                Composer    = composer;
                Unhandled   = false;
                ReturnValue = Invoke(targetMethod, target, composer);
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

        private bool IsTargetAccepted(object target)
        {
            return _semantics.HasOption(CallbackOptions.Strict)
                 ? Protocol.IsTopLevelInterface(target.GetType())
                 : _semantics.HasOption(CallbackOptions.Duck)
                || Protocol.IsInstanceOfType(target);
        }

        protected object Invoke(
            MethodInfo method, object target, IHandler composer)
        {
            var options = composer.GetFilterOptions();
            if (options?.SuppressFilters == true)
                return method.Invoke(target, Binding, null, Arguments, null);

            var filters = composer.GetOrderedFilters(options,
                    FilterAttribute.GetFilters(target.GetType(), true),
                    FilterAttribute.GetFilters(method))
                .OfType<IFilter<HandleMethod, object>>()
                .ToArray();

            if (filters.Length == 0)
                return method.Invoke(target, Binding, null, Arguments, null);

            var index = -1;
            FilterDelegate<object> next = null;
            next = proceed =>
            {
                if (!proceed)
                    return RuntimeHelper.GetDefault(method.ReturnType);
                return ++index < filters.Length
                        ? filters[index].Filter(this, composer, next) 
                        : method.Invoke(target, Binding, null, Arguments, null);
            };

            return next();
        }

        bool ICallbackDispatch.Dispatch(Handler handler, bool greedy, IHandler composer)
        {
            var surrogate = handler.Surrogate;
            var handled = surrogate != null && InvokeOn(surrogate, composer);
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

        [ThreadStatic] public static IHandler Composer;
        [ThreadStatic] public static bool     Unhandled;

        private const BindingFlags Binding = BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.NonPublic;
    }
}
