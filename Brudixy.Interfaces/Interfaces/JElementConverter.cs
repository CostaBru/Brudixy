using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Brudixy.Interfaces.Interfaces
{
    public class JElementConverter : JsonConverter<JElement>
    {
        public override void Write(Utf8JsonWriter writer, JElement value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", value.Name);

            if (value.Value != null)
            {
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, value.Value, options);
            }

            if (value.Attributes != null && value.Attributes.Count > 0)
            {
                writer.WritePropertyName("Attributes");
                JsonSerializer.Serialize(writer, value.Attributes, options);
            }

            if (value.Elements != null && value.Elements.Count > 0)
            {
                writer.WritePropertyName("Elements");
                JsonSerializer.Serialize(writer, value.Elements, options);
            }

            writer.WriteEndObject();
        }

        public override JElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadInternal(ref reader, options);
        }

        private JElement ReadInternal(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.None)
            {
                if (!reader.Read())
                {
                    throw new JsonException();
                }
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var jElement = new JElement();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return jElement;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "Name":
                        jElement.Name = reader.GetString();
                        break;
                    case "Value":
                        jElement.Value = JsonSerializer.Deserialize<object>(ref reader, options);
                        break;
                    case "Attributes":
                        jElement.Attributes = JsonSerializer.Deserialize<List<JAttribute>>(ref reader, options);
                        break;
                    case "Elements":
                        jElement.Elements = JsonSerializer.Deserialize<List<JElement>>(ref reader, options);
                        break;
                }
            }

            throw new JsonException();
        }
    }
}