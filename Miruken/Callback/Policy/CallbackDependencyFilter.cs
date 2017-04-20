namespace Miruken.Callback.Policy
{
    using System;

    public class CallbackDependencyFilter : ICallbackFilter
    {
        private readonly ICallbackFilter _filter;
        private readonly Func<object, object> _dependency;

        public CallbackDependencyFilter(
            ICallbackFilter filter,
            Func<object, object> dependency)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));
            _filter     = filter;
            _dependency = dependency;
        }

        public object Filter(
            object callback, IHandler composer, ProceedDelegate proceed)
        {
            return _filter.Filter(_dependency(callback), composer, proceed);
        }
    }
}
