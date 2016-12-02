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

        public object Callback { get; private set; }

        public Type ResultType
        {
            get
            {
                var cb = Callback as ICallback;
                return cb != null ? cb.ResultType : null;
            }
        }

        public object Result
        {
            get
            {
                var cb = Callback as ICallback;
                return cb != null ? cb.Result : null;
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
            return composition != null && composition.Callback is T;
        }
    }
}
