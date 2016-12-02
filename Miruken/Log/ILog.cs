using System;
using System.Runtime.InteropServices;
using Miruken.Callback;
using Miruken.Concurrency;

namespace Miruken.Log
{
    #region Protocol
    [ComImport,
     Guid(Protocol.Guid),
     CoClass(typeof(LogProtocol))]
    #endregion
    public interface ILog
    {
        Promise Log(LogLevel level, string format, params object[] args);
        Promise Log(LogLevel level, Exception exception, string format, params object[] args);
    }

    #region LogProtocol

    public class LogProtocol : Protocol, ILog
    {
        public LogProtocol(IProtocolAdapter adapter) : base(adapter)
        {
        }

        Promise ILog.Log(LogLevel level, string format, params object[] args)
        {
            return Do((ILog p) => p.Log(level, format, args));
        }

        Promise ILog.Log(LogLevel level, Exception exception, string format, params object[] args)
        {
            return Do((ILog p) => p.Log(level, exception, format, args));
        }
    }

    #endregion
}
