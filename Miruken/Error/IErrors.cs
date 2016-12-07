using System;

namespace Miruken.Error
{
    public interface IErrors
    {
        bool HandleException(Exception exception, object context = null);
    }
}
