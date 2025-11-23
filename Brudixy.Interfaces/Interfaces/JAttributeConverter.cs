using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brudixy.Interfaces
{
    public class JAttributeConverter : JsonConverter<JAttribute>
    {
        public override JAttribute Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string name = reader.GetString();
            reader.Read();
            object value = JsonSerializer.Deserialize<object>(ref reader, options);

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            return new JAttribute(name, value);
        }

        public override void Write(Utf8JsonWriter writer, JAttribute value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(value.Name);
            JsonSerializer.Serialize(writer, value.Value, options);
            writer.WriteEndObject();
        }
    }
}