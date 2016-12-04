using System;
using Miruken.Concurrency;

namespace Miruken.Callback
{
    public abstract class HandleMethod : ICallback
    {
        public abstract Type ResultType { get; }

        public object Result
        {
            get { return ReturnValue; }
            set { ReturnValue = value; }
        }

        public object ReturnValue { get; set; }

        public Exception Exception { get; set; }

        public abstract Type TargetType { get; }

        public abstract bool InvokeOn(object target, ICallbackHandler composer);

        [ThreadStatic] public static ICallbackHandler Composer;

        public static ICallbackHandler RequireComposer()
        {
            var composer = Composer;
            if (composer == null)
                throw new InvalidOperationException(
                    "Composer not availanle.  Did you call this method directly?");
            return composer;
        }

        [ThreadStatic] public static bool Unhandled;
    }

    public class HandleAction<T> : HandleMethod
        where T : class
    {
        private readonly Action<T> Action;

        public HandleAction(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Action = action;
        }

        public override Type TargetType => typeof (T);

        public override Type ResultType => null;

        public override bool InvokeOn(object target, ICallbackHandler composer)
        {
            var receiver = target as T;
            if (receiver == null) return false;

            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;

            try
            {
                Composer  = composer;
                Unhandled = false;
                Action(receiver);
                return !Unhandled;
            }
            catch (Exception exception)
            {
                Exception = exception;
                throw;
            }
            finally
            {
                Unhandled = oldUnhandled;
                Composer  = oldComposer;
            }
        }
    }

    public class HandleFunc<T, R> : HandleMethod
         where T : class
    {
        private readonly Func<T, R> _func;

        public HandleFunc(Func<T, R> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            _func = func;
        }

        public override Type TargetType => typeof(T);

        public override Type ResultType => typeof(R);

        public override bool InvokeOn(object target, ICallbackHandler composer)
        {
            var receiver = target as T;
            if (receiver == null) return false;

            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;

            try
            {
                Composer   = composer;
                Unhandled = false;
                var returnValue = _func(receiver);
                if (Unhandled) return false;
                ReturnValue = returnValue;
                return true;
            }
            catch (Exception exception)
            {
                Exception = exception;
                var resultType = typeof (R);
                if (!typeof(Promise).IsAssignableFrom(resultType))
                    throw;
                ReturnValue = Promise.Rejected(exception).Coerce(resultType);
                return true;
            }
            finally
            {
                Unhandled = oldUnhandled;
                Composer   = oldComposer;
            }
        }
    }
}
