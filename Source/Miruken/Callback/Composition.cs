namespace Miruken.Callback
{
    using System;
    using Policy;

    public class Composition 
        : ICallback, IDispatchCallback, IResolveCallback,
          IFilterCallback, IBatchCallback
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

        bool IFilterCallback.AllowFiltering =>
            (Callback as IFilterCallback)?.AllowFiltering != false;

        bool IBatchCallback.AllowBatching =>
            (Callback as IBatchCallback)?.AllowBatching != false;

        object IResolveCallback.GetResolveCallback()
        {
            var resolve = (Callback as IResolveCallback)?.GetResolveCallback();
            if (ReferenceEquals(resolve, Callback)) return this;
            if (resolve == null)
                resolve = Resolving.GetDefaultResolvingCallback(Callback);
            return new Composition(resolve);
        }

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
