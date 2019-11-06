namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;

    [Unmanaged]
    public class ServiceProvider : Handler
    {
        private readonly IServiceProvider _provider;

        public ServiceProvider(IServiceProvider provider)
        {
            _provider = provider ??
                throw new ArgumentNullException(nameof(provider));
        }

        [Provides]
        public object Provide(Inquiry inquiry)
        {
            var type = inquiry.Key as Type;
            if (type != null && inquiry.Metadata.IsEmpty)
            {
                if (inquiry.Many)
                {
                    var manyType = typeof(IEnumerable<>).MakeGenericType(type);
                    return _provider.GetService(manyType);
                }
                return _provider.GetService(type);
            }
            return null;
        }
    }
}
