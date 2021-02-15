namespace Miruken.Http
{
    using System.Net.Http.Formatting;
    using Format;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public static class HttpFormatters
    {
        public static readonly XmlMediaTypeFormatter  Xml  = new();
        public static readonly JsonMediaTypeFormatter Json = new()
        {
            SerializerSettings =
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver  = new CamelCasePropertyNamesContractResolver()
            }
        };

        public static readonly JsonMediaTypeFormatter JsonTyped = new()
        {
            SerializerSettings =
            {
                NullValueHandling              = NullValueHandling.Ignore,
                TypeNameHandling               = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ContractResolver               = new CamelCasePropertyNamesContractResolver()
            }
        };

        public static readonly ReadWriteFormUrlEncodedMediaTypeFormatter FormUrl =
            new();

        public static readonly HttpRouteMediaTypeFormatter Route = new();

        public static readonly MediaTypeFormatterCollection Default =
            new(new MediaTypeFormatter[]
            {
                Json, Xml, FormUrl
            });
    }
}
