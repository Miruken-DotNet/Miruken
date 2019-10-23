namespace Miruken.Http.Format
{
    using System;
    using Functional;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class EitherJsonConverter : JsonConverter
    {
        public static readonly EitherJsonConverter Instance = new EitherJsonConverter();

        private EitherJsonConverter()
        {          
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IEither).IsAssignableFrom(objectType);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is IEither either)) return;
            var args = either.GetType().GetGenericArguments();
            writer.WriteStartObject();
            writer.WritePropertyName("isLeft");
            writer.WriteValue(either.IsLeft);
            writer.WritePropertyName("value");
            var type = either.IsLeft ? args[0] : args[1];
            serializer.Serialize(writer, either.Value, type);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, 
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            var json   = JObject.Load(reader);
            var isLeft = json["isLeft"].Value<bool>();
            var either = json["value"].CreateReader();
            var args   = objectType.GetGenericArguments();
            var type   = isLeft ? args[0] : args[1];
            var value  = serializer.Deserialize(either, type);
            var ctor   = objectType.GetConstructor(new [] { type });
            return (IEither) ctor?.Invoke(new[] {value});
        }
    }
}
