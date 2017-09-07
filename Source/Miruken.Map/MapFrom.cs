namespace Miruken.Map
{
    using System;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Concurrency;

    public class MapFrom : 
        ICallback, IAsyncCallback, IDispatchCallback
    {
        private object _result;

        public MapFrom(object source, object format,
                       object typeOrInstance)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            Source         = source;
            Format         = format;
            TypeOrInstance = typeOrInstance;
        }

        public object Source         { get; }
        public object Format         { get; }
        public object TypeOrInstance { get; }
        public bool   WantsAsync     { get; set; }
        public bool   IsAsync        { get; private set; }

        public Type ResultType => 
            WantsAsync || IsAsync ? typeof(Promise) : _result?.GetType();

        public CallbackPolicy Policy => MapsAttribute.Policy;

        public object Result
        {
            get
            {
                if (WantsAsync && !IsAsync)
                    _result = Promise.Resolved(_result);
                return _result;
            }
            set
            {
                _result = value;
                IsAsync = _result is Promise || _result is Task;
            }
        }

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            return Policy.Dispatch(
                handler, this, greedy, composer, SetMapping);
        }

        private bool SetMapping(object mapping, bool strict)
        {
            var mapped = mapping != null;
            if (mapped) Result = mapping;
            return mapped;
        }
    }
}
