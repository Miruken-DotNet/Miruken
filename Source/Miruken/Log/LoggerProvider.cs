namespace Miruken.Log;

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
    public ILogger GetContextualLogger(Inquiry inquiry) =>
        inquiry.Parent?.Key is not Type owner ? null : _factory.CreateLogger(owner);
}