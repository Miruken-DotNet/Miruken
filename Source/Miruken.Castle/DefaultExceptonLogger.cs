namespace Miruken.Castle
{
    using System;
    using System.Linq;
    using System.Security.Authentication;
    using global::Castle.Core.Logging;

    public class DefaultExceptonLogger : IExceptionLogger
    {
        public virtual LogExceptionDelegate GetLogger(
            Exception exception, ILogger logger)
        {
            if (IsWarning(exception))
                return GetWarningLogger(logger);
             return logger.IsErrorEnabled
                  ? logger.ErrorFormat
                  : (LogExceptionDelegate)null;
        }

        protected virtual LogExceptionDelegate GetWarningLogger(ILogger logger)
        {
            return logger.IsInfoEnabled ? logger.InfoFormat
                 : (LogExceptionDelegate)null;
        }

        protected virtual bool IsWarning(Exception exception)
        {
            return WarningExceptions.Any(wex => wex.IsInstanceOfType(exception));
        }

        private static readonly Type[] WarningExceptions =
        {
            typeof(ArgumentException),
            typeof(InvalidOperationException),
            typeof(AuthenticationException),
            typeof(UnauthorizedAccessException)
        };
    }
}
