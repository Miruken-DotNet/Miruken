namespace Miruken.Api.Route
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        Inherited = false)]
    public class RoutesAttribute : Attribute, IFilterProvider
    {
        private readonly RoutesFilter[] _filters;

        public RoutesAttribute(params string[] schemes)
        {
            if (schemes == null || schemes.Length == 0)
                throw new ArgumentException("Schemes cannot be empty", nameof(schemes));
            _filters = new [] { new RoutesFilter(schemes) };
        }

        public bool Required => true;

        public IEnumerable<IFilter> GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            Type callbackType, IHandler composer)
        {
            return _filters;
        }

        private class RoutesFilter : IFilter<Routed, object>
        {
            private readonly string[] _schemes;

            public int? Order { get; set; } = Stage.Logging - 1;

            public RoutesFilter(string[] schemes)
            {
                _schemes = schemes;
            }

            public Task<object> Next(Routed routed,
                object rawCallback, MemberBinding method,
                IHandler composer, Next<object> next,
                IFilterProvider provider)
            {
                var matches = Array.IndexOf(_schemes, GetScheme(routed)) >= 0;
                if (matches)
                {
                    var batch = composer.GetBatch<BatchRouter>();
                    if (batch != null)
                        return composer.EnableFilters().CommandAsync(
                            new Batched<Routed>(routed, rawCallback));
                }
                return next(composer.EnableFilters(), matches);
            }

            private static string GetScheme(Routed routed)
            {
                return Uri.TryCreate(routed.Route, UriKind.Absolute, out var uri)
                     ? uri.Scheme : null;
            }
        }
    }
}
