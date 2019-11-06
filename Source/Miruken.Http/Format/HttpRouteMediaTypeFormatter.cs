namespace Miruken.Http.Format
{
    using System;
    using System.Net.Http.Formatting;
    using Callback;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class HttpRouteMediaTypeFormatter : JsonMediaTypeFormatter
    {
        public HttpRouteMediaTypeFormatter()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            SerializerSettings.Converters.Add(EitherJsonConverter.Instance);
        }

        public HttpRouteMediaTypeFormatter(
            IHandler handler,
            BaseJsonMediaTypeFormatter source = null)
            : this()
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            source?.SerializerSettings?.Copy(SerializerSettings);
            SerializerSettings.Converters.Add(new ExceptionJsonConverter(handler));
        }
    }
}
