namespace Miruken.Callback
{
    using System;

    public class Composition 
        : Trampoline, ICallback, IResolveCallback,
          IFilterCallback, IBatchCallback
    {
        public Composition(object callback)
            : base(callback)
        {
        }

        protected Composition()
        {         
        }

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
                if (Callback is ICallback cb)
                    cb.Result = value;
            }
        }

        bool IFilterCallback.CanFilter =>
            (Callback as IFilterCallback)?.CanFilter != false;

        bool IBatchCallback.CanBatch =>
            (Callback as IBatchCallback)?.CanBatch != false;

        object IResolveCallback.GetResolveCallback()
        {
            var resolve = (Callback as IResolveCallback)?.GetResolveCallback();
            if (ReferenceEquals(resolve, Callback)) return this;
            if (resolve == null)
                resolve = Resolving.GetDefaultResolvingCallback(Callback);
            return new Composition(resolve);
        }

        public static bool IsComposed<T>(object callback)
            where T : class
        {
            var composition = callback as Composition;
            return composition?.Callback is T;
        }
    }
}
