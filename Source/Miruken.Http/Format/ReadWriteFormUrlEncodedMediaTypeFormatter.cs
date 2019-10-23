namespace Miruken.Http.Format
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;
    using Infrastructure;
    using Newtonsoft.Json.Linq;

    public class ReadWriteFormUrlEncodedMediaTypeFormatter
        : FormUrlEncodedMediaTypeFormatter
    {
        public string DateTimeFormat { get; set; }

        public override bool CanWriteType(Type type)
        {
            return !type.IsSimpleType();
        }

        public override Task WriteToStreamAsync(
            Type type, object value, Stream writeStream, HttpContent content,
            TransportContext transportContext)
        {
            var keyValues = ToKeyValue(value);
            var formUrlEncodedContent = new FormUrlEncodedContent(keyValues);
            return formUrlEncodedContent.CopyToAsync(writeStream);
        }

        private IDictionary<string, string> ToKeyValue(object metaToken)
        {
            if (metaToken is IDictionary<string, string> tokens)
                return tokens;

            while (true)
            {
                if (metaToken == null)
                    return null;

                if (!(metaToken is JToken token))
                {
                    metaToken = JObject.FromObject(metaToken);
                    continue;
                }

                if (token.HasValues)
                {
                    var contentData = new Dictionary<string, string>();
                    return token.Children().ToList()
                        .Select(ToKeyValue)
                        .Where(childContent => childContent != null)
                        .Aggregate(contentData,
                            (current, childContent) => current.Concat(childContent)
                        .ToDictionary(k => k.Key, v => v.Value));
                }

                var jValue = token as JValue;
                if (jValue?.Value == null)
                    return null;

                var value = jValue.Type == JTokenType.Date 
                    ? jValue.ToString(DateTimeFormat ?? "o", CultureInfo.InvariantCulture) 
                    : jValue.ToString(CultureInfo.InvariantCulture);

                return new Dictionary<string, string> {{token.Path, value}};
            }
        }
    }
}
