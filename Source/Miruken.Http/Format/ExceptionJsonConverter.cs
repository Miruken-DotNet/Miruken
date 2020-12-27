namespace Miruken.Http.Format
{
    using System;
    using Callback;
    using Map;
    using Newtonsoft.Json;

    public class ExceptionJsonConverter : JsonConverter
    {
        private readonly IHandler _handler;

        public ExceptionJsonConverter(IHandler handler)
        {
            _handler = handler 
                ?? throw new ArgumentNullException(nameof(handler));
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Exception).IsAssignableFrom(objectType);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            var stub = _handler.Map<object>(value, typeof(Exception));
            serializer.Serialize(writer, stub, typeof(ForceTypeInformation));
        }

        public override object ReadJson(JsonReader reader, 
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            var stub = serializer.Deserialize(reader);
            return _handler.Map<Exception>(stub, typeof(Exception));
        }
        
        private class ForceTypeInformation {}
    }
}
