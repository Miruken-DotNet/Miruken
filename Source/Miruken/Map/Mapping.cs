namespace Miruken.Map
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Concurrency;
    using Infrastructure;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Mapping : ICallback,
        IAsyncCallback, IDispatchCallback
    {
        private object _result;

        public Mapping(object source, object typeOrTarget,
                       object format = null)
        {
            Source = source 
                ?? throw new ArgumentNullException(nameof(source));
            TypeOrTarget = typeOrTarget
                ?? throw new ArgumentNullException(nameof(typeOrTarget));
            Format = format;
        }

        public object Source       { get; }
        public object TypeOrTarget { get; }
        public object Format       { get; }
        public bool   WantsAsync   { get; set; }
        public bool   IsAsync      { get; private set; }

        public Type Type =>
            TypeOrTarget as Type ?? TypeOrTarget.GetType();

        public object Target => 
            TypeOrTarget is Type ? null : TypeOrTarget;

        public Type ResultType => 
            WantsAsync || IsAsync ? typeof(Promise) : _result?.GetType();

        public CallbackPolicy Policy => Maps.Policy;

        public object Result
        {
            get
            {
                if (_result == null)
                    _result = RuntimeHelper.GetDefault(Type);

                if (IsAsync)
                {
                    if (!WantsAsync)
                        _result = (_result as Promise)?.Wait();
                }
                else if (WantsAsync)
                    _result = Promise.Resolved(_result);

                return _result;
            }
            set
            {
                _result = value ?? RuntimeHelper.GetDefault(Type);
                IsAsync = _result is Promise || _result is Task;
            }
        }

        public bool Dispatch(object handler, ref bool greedy, IHandler composer)
        {
            return Policy.Dispatch(
                handler, this, greedy, composer, SetMapping)
                || _result != null;
        }

        private bool SetMapping(object mapping, bool strict)
        {
            var mapped = mapping != null;
            if (mapped) Result = mapping;
            return mapped;
        }

        private string DebuggerDisplay
        {
            get
            {
                var format = Format != null ? $"as {Format}" : "";
                return $"Mapping | {Source} to {TypeOrTarget}{format}";
            }
        }
    }
}
