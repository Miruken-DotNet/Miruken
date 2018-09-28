namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using global::Castle.Core.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class LogFilter<TRequest, TResponse>
        : LogFilter, IFilter<TRequest, TResponse>
    {
        public int? Order { get; set; } = Stage.Logging;

        public ILoggerFactory LoggerFactory { get; set; }

        public async Task<TResponse> Next(
            TRequest request, MemberBinding member,
            IHandler composer, Next<TResponse> next,
            IFilterProvider provider)
        {
            var logger = GetLogger(member);
            var debug  = logger.IsDebugEnabled;
            var start  = Stopwatch.GetTimestamp();

            if (debug)
                logger.DebugFormat("Handling {0}", Describe(request));

            try
            {
                var response = await next();

                if (debug)
                {
                    var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                    logger.DebugFormat("Completed {0} in {1}{2}{3}",
                        PrettyName(request.GetType()),
                        FormatElapsedMilliseconds(elapsedMs),
                        response != null ? " with " : "",
                        Describe(response));
                }

                return response;
            }
            catch (Exception ex) when (LogException(request, member,
                GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex))
            {
                // Never caught, because `LogException()` returns false.
            }

            return default;
        }

        private bool LogException(TRequest request,
            MemberBinding member, double elapsedMs, Exception ex)
        {
            var exceptionLogger = LoggerFactory?.Create(ex.GetType());
            if (exceptionLogger?.IsErrorEnabled == true)
            {
                exceptionLogger.ErrorFormat(ex, "{0} Failed {1} in {2}",
                    member.Dispatcher.Member.ReflectedType?.FullName,
                    Describe(request),
                    FormatElapsedMilliseconds(elapsedMs));
            }
            ex.Data[Stage.Logging] = true;
            return false;
        }

        private static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }

        private static string FormatElapsedMilliseconds(double elapsedMs)
        {
            if (elapsedMs > 60000)
                return $"{(elapsedMs / 60000):0.00} min";
            return elapsedMs > 1000
                ? $"{(elapsedMs / 1000):0.00} sec"
                : $"{elapsedMs:0.00} ms";
        }

        private static string Describe(object instance)
        {
            if (instance == null) return "";
            var description = new StringBuilder(PrettyName(instance.GetType()));
            try
            {
                description.Append(" ")
                    .Append(JsonConvert.SerializeObject(instance, JsonSettings))
                    .Replace("\"", "");
            }
            catch
            {
                description.Append(" ").Append(instance);
            }

            return description.ToString();
        }

        private ILogger GetLogger(MemberBinding member)
        {
            var type = member.Dispatcher.Member.ReflectedType;
            return LoggerFactory?.Create(type) ?? NullLogger.Instance;
        }
    }

    #region Formatting

    public abstract class LogFilter
    {
        protected static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                Formatting            = Formatting.Indented,
                DateFormatString      = "MM-dd-yyyy hh:mm:ss",
                NullValueHandling     = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters            = {
                    new StringEnumConverter(),
                    new ByteArrayFormatter()
                }
            };

        protected static string PrettyName(Type type)
        {
            if (type.IsGenericType)
            {
                return (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    ? $"{PrettyName(Nullable.GetUnderlyingType(type))}?"
                    : $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GenericTypeArguments.Select(PrettyName).ToArray())}>";
            }
            if (type.IsArray)
                return $"{PrettyName(type.GetElementType())}[]";

            return SimpleNames.TryGetValue(type, out var name) ? name : type.Name;
        }

        protected static readonly Dictionary<Type, string>
            SimpleNames = new Dictionary<Type, string>
            {
                { typeof (bool),    "bool" },
                { typeof (byte),    "byte" },
                { typeof (char),    "char" },
                { typeof (decimal), "decimal" },
                { typeof (double),  "double" },
                { typeof (float),   "float" },
                { typeof (int),     "int" },
                { typeof (long),    "long" },
                { typeof (sbyte),   "sbyte" },
                { typeof (short),   "short" },
                { typeof (string),  "string "},
                { typeof (uint),    "uint" },
                { typeof (ulong),   "ulong" },
                { typeof (ushort),  "ushort" }
            };

        protected class ByteArrayFormatter : JsonConverter
        {
            public override bool CanRead => false;

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(byte[]);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var bytes = (byte[])value;
                writer.WriteValue($"({bytes.Length} bytes)");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException("Unnecessary because CanRead is false.");
            }
        }
    }

    #endregion
}
