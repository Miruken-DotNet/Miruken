namespace Miruken.Http.Format
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Functional;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class EitherJsonConverter : JsonConverter
    {
        public static readonly EitherJsonConverter Instance = new();

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
            var isLeft = either is IEither.ILeft;
            var args = either.GetType().GetGenericArguments();
            writer.WriteStartObject();
            writer.WritePropertyName("isLeft");
            writer.WriteValue(isLeft);
            writer.WritePropertyName("value");
            var type = isLeft ? args[0] : args[1];
            serializer.Serialize(writer, either.Value, type);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, 
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            var json   = JObject.Load(reader);
            var isLeft = json["isLeft"]?.Value<bool>() ??
                throw new JsonReaderException("Expected 'isLeft' property for Either.");
            var either = json["value"]?.CreateReader() ??
                throw new JsonReaderException("Expected 'value' property for Either.");
            var args   = objectType.GetGenericArguments();
            var type   = isLeft ? args[0] : args[1];
            var value  = serializer.Deserialize(either, type);
            ConstructorInfo ctor;
            if (isLeft)
            {
                if (typeof(IEither.ILeft).IsAssignableFrom(objectType))
                {
                    ctor = objectType.GetConstructor(new[] {type});
                }
                else if (typeof(IEither.IRight).IsAssignableFrom(objectType))
                {
                    throw new InvalidOperationException(
                        $"Expected an IEither.ILeft type but given {objectType.FullName}.");
                }
                else
                {
                    var leftType = objectType.GetNestedTypes()
                        .FirstOrDefault(x => typeof(IEither.ILeft).IsAssignableFrom(x))
                        ?.MakeGenericType(objectType.GetGenericArguments());
                    if (leftType == null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to infer an IEther.ILeft type for {objectType.FullName}.");
                    }
                    ctor = leftType.GetConstructor(new[] { type });
                }
            }
            else if (typeof(IEither.IRight).IsAssignableFrom(objectType))
            {
                ctor = objectType.GetConstructor(new[] {type});
            }
            else if (typeof(IEither.ILeft).IsAssignableFrom(objectType))
            {
                throw new InvalidOperationException(
                    $"Expected an IEither.IRight type but given {objectType.FullName}.");
            }
            else
            {
                var rightType = objectType.GetNestedTypes()
                    .FirstOrDefault(x => typeof(IEither.IRight).IsAssignableFrom(x))
                    ?.MakeGenericType(objectType.GetGenericArguments());
                if (rightType == null)
                {
                    throw new InvalidOperationException(
                        $"Unable to infer an IEther.IRight type for {objectType.FullName}.");
                }
                ctor = rightType.GetConstructor(new[] { type });
            }
            return (IEither) ctor?.Invoke(new[] {value});
        }
    }
}
