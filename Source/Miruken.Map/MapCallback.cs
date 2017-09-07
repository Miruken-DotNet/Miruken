namespace Miruken.Map
{
    using System;
    using Callback;
    using Callback.Policy;

    public abstract class MapCallback : ICallback
    {
        protected MapCallback(object format)
        {
            Format = format;
        }

        public object Format { get; }
        public object Result { get; set; }

        public Type ResultType => Result?.GetType();

        protected bool SetMapping(object mapping, bool strict)
        {
            var mapped = mapping != null;
            if (mapped)            
                Result = mapping;
            return mapped;
        }
    }

    public class MapFrom : MapCallback, IDispatchCallback
    {
        public MapFrom(object source, object format)
            : base(format)
        {
            Source = source;
        }

        public object Source { get; }

        public CallbackPolicy Policy => MapsFromAttribute.Policy;

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            return Policy.Dispatch(
                handler, this, greedy, composer, SetMapping);
        }
    }

    public class MapTo : MapCallback, IDispatchCallback
    {
        public MapTo(object formattedValue, 
            object typeOrInstance, object format)
            : base(format)
        {
            FormattedValue = formattedValue;
            TypeOrInstance = typeOrInstance;
        }

        public object FormattedValue { get; }
        public object TypeOrInstance { get; }

        public CallbackPolicy Policy => MapsToAttribute.Policy;

        bool IDispatchCallback.Dispatch(
            object handler, ref bool greedy, IHandler composer)
        {
            return Policy.Dispatch(
                handler, this, greedy, composer, SetMapping);
        }
    }
}
