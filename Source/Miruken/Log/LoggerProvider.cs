#if NETSTANDARD
namespace Miruken.Log
{
    using System;
    using Callback;
    using Microsoft.Extensions.Logging;

    public class LoggerProvider : Handler
    {
        private readonly ILoggerFactory _factory;

        public LoggerProvider(ILoggerFactory factory)
        {
            _factory = factory;
        }

        [Provides]
        public ILogger GetContextualLogger(Inquiry inquiry)
        {
            var owner = inquiry.Parent?.Key as Type;
            return owner == null ? null : _factory.CreateLogger(owner);
        }
    }
}
#endif
