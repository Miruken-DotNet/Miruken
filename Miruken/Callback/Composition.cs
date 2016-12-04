using System;

namespace Miruken.Callback
{
    public class Composition : ICallback
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

        public static bool IsComposed<T>(object callback)
            where T : class
        {
            var composition = callback as Composition;
            return composition?.Callback is T;
        }
    }
}
