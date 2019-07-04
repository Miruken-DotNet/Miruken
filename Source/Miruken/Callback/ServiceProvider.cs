namespace Miruken.Callback
{
    using System;

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
            return type != null ? _provider.GetService(type) : null;
        }
    }
}
