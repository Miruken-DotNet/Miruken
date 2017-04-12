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

        public bool Accepts(object callback, IHandler composer)
        {
            return _filter.Accepts(_dependency(callback), composer);
        }
    }
}
