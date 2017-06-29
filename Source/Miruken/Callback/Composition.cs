using System;

namespace Miruken.Callback
{
    using Policy;

    public class Composition : ICallback, IDispatchCallback
    {
        public Composition(object callback)
        {
            Callback = callback;
        }

        protected Composition()
        {         
        }

        public object Callback { get; }

        public Type ResultType
        {
            get
            {
                var cb = Callback as ICallback;
                return cb?.ResultType;
            }
        }

        public object Result
        {
            get
            {
                var cb = Callback as ICallback;
                return cb?.Result;
            }

            set
            {
                var cb = Callback as ICallback;
                if (cb != null)
                    cb.Result = value;
            }
        }

        CallbackPolicy IDispatchCallback.Policy =>
            (Callback as IDispatchCallback)?.Policy;

        bool IDispatchCallback.Dispatch(object handler, ref bool greedy, IHandler composer)
        {
            var callback = Callback;
            return callback != null &&
                   Handler.Dispatch(handler, callback, ref greedy, composer);
        }

        public static bool IsComposed<T>(object callback)
            where T : class
        {
            var composition = callback as Composition;
            return composition?.Callback is T;
        }
    }
}
