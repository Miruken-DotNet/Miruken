namespace Miruken.Castle
{
    using System;
    using global::Castle.Core.Logging;

    public delegate void LogExceptionDelegate(
        Exception exception, string format, params object[] args);

    public interface IExceptionLogger
    {
        LogExceptionDelegate GetLogger(Exception exception, ILogger logger);
    }
}
