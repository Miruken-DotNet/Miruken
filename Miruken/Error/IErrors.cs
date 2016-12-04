using System;
using System.Runtime.InteropServices;
using SixFlags.CF.Miruken.Callback;

namespace SixFlags.CF.Miruken.Error
{
    #region Protocol
    [ComImport,
     Guid(Protocol.Guid),
     CoClass(typeof(ErrorsProtocol))]
    #endregion
    public interface IErrors
    {
        bool HandleException(Exception exception, object context);
    }

    #region ErrorsProtocol

    public class ErrorsProtocol : Protocol, IErrors
    {
        public ErrorsProtocol(IProtocolAdapter adapter) : base(adapter)
        {
        }

        bool IErrors.HandleException(Exception exception, object context)
        {
            return Do((IErrors p) => p.HandleException(exception, context));
        }
    }

    #endregion
}
