using System;
using System.Threading;
using SixFlags.CF.Miruken.Concurrency;

namespace SixFlags.CF.Miruken.Callback
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

        public static ICallbackHandler Composer
        {
            get
            {
                var data = HandleMethodData.Get(false);
                return data != null ? data.Composer : null;
            }
            protected set { HandleMethodData.Get(true).Composer = value; }
        }

        public static ICallbackHandler RequireComposer()
        {
            var composer = Composer;
            if (composer == null)
                throw new InvalidOperationException(
                    "Composer not availanle.  Did you call this method directly?");
            return composer;
        }

        public static bool Unhandled
        {
            get
            {
                var data = HandleMethodData.Get(false);
                return data != null && data.Unhandled;
               
            }
            set { HandleMethodData.Get(true).Unhandled = value; }
        }
    }

    #region HandleMethodData

    internal class HandleMethodData
    {
        public ICallbackHandler Composer;
        public bool             Unhandled;

        public static HandleMethodData Get(bool create)
        {
            var data = (HandleMethodData)Thread.GetData(Slot);
            if (data != null) return data;
            data = new HandleMethodData();
            Thread.SetData(Slot, data);
            return data;
        }

        static readonly LocalDataStoreSlot Slot = Thread.AllocateDataSlot();
    }

    #endregion

    public class HandleAction<T> : HandleMethod
        where T : class
    {
        private readonly Action<T> Action;

        public HandleAction(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            Action = action;
        }

        public override Type TargetType
        {
            get { return typeof (T); }
        }

        public override Type ResultType
        {
            get { return null; }
        }

        public override bool InvokeOn(object target, ICallbackHandler composer)
        {
            var receiver = target as T;
            if (receiver == null) return false;

            var threadData   = HandleMethodData.Get(true);
            var oldComposer  = threadData.Composer;
            var oldUnhandled = threadData.Unhandled;

            try
            {
                threadData.Composer  = composer;
                threadData.Unhandled = false;
                Action(receiver);
                return !threadData.Unhandled;
            }
            catch (Exception exception)
            {
                Exception = exception;
                throw;
            }
            finally
            {
                threadData.Unhandled = oldUnhandled;
                threadData.Composer  = oldComposer;
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
                throw new ArgumentNullException("func");
            _func = func;
        }

        public override Type TargetType
        {
            get { return typeof(T); }
        }

        public override Type ResultType
        {
            get { return typeof(R); }
        }

        public override bool InvokeOn(object target, ICallbackHandler composer)
        {
            var receiver = target as T;
            if (receiver == null) return false;

            var threadData   = HandleMethodData.Get(true);
            var oldComposer  = threadData.Composer;
            var oldUnhandled = threadData.Unhandled;

            try
            {
                threadData.Composer   = composer;
                threadData.Unhandled = false;
                var returnValue = _func(receiver);
                if (threadData.Unhandled)
                    return false;
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
                threadData.Unhandled = oldUnhandled;
                threadData.Composer   = oldComposer;
            }
        }
    }
}
