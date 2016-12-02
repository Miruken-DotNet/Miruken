using System;
using System.Linq;
using Miruken.Concurrency;
using Miruken.Validation;

namespace Miruken.Log
{
    public static class LoggingExtensions
    {
        public static Promise Trace(this ILog log, string format, params object[] args)
        {
            return Log(log, LogLevel.Trace, null, format, args);
        }

        public static Promise Trace(this ILog log, Exception exception, string format, params object[] args)
        {
            return Log(log, LogLevel.Trace, exception, format, args);
        }

        public static Promise Debug(this ILog log, string format, params object[] args)
        {
            return Log(log, LogLevel.Debug, null, format, args);
        }

        public static Promise Debug(this ILog log, Exception exception, string format, params object[] args)
        {
            return Log(log, LogLevel.Debug, exception, format, args);
        }

        public static Promise Info(this ILog log, string format, params object[] args)
        {
            return Log(log, LogLevel.Info, null, format, args);
        }

        public static Promise Info(this ILog log, Exception exception, string format, params object[] args)
        {
            return Log(log, LogLevel.Info, exception, format, args);
        }

        public static Promise Warn(this ILog log, string format, params object[] args)
        {
            return Log(log, LogLevel.Warn, null, format, args);
        }

        public static Promise Warn(this ILog log, Exception exception, string format, params object[] args)
        {
            return Log(log, LogLevel.Warn, exception, format, args);
        }

        public static Promise Error(this ILog log, string format, params object[] args)
        {
            return Log(log, LogLevel.Error, null, format, args);
        }

        public static Promise Error(this ILog log, Exception exception, string format, params object[] args)
        {
            return Log(log, LogLevel.Error, exception, format, args);
        }

        public static Promise Fatal(this ILog log, string format, params object[] args)
        {
            return Log(log, LogLevel.Fatal, null, format, args);
        }

        public static Promise Fatal(this ILog log, Exception exception, string format, params object[] args)
        {
            return Log(log, LogLevel.Fatal, exception, format, args);
        }

        public static Promise Exception(this ILog log, Exception exception, string format, params object[] args)
        {
            var level =
                WarningExceptions.Any(wex => wex.IsInstanceOfType(exception))
                    ? LogLevel.Warn
                    : LogLevel.Error;

            return Log(log, level, exception, format, args);
        }

        private static Promise Log(ILog log, LogLevel level, Exception exception, string format, params object[] args)
        {
            return log != null
                 ? log.Log(level, exception, format, args)
                 : Promise.Empty;
        }

        private static readonly Type[] WarningExceptions =
        {
            typeof(ArgumentException),
            typeof(InvalidOperationException),
            typeof(ValidationException)
        };
    }
}
