using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentflow.Server.Serialization;

public sealed class PolymorphicJsonConverter<T> : JsonConverter<T>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(T).IsAssignableFrom(typeToConvert);
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        foreach (var property in value.GetType().GetProperties())
        {
            if (!property.CanRead)
            {
                continue;
            }
            var propertyValue = property.GetValue(value);
            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, propertyValue, options);
        }
        writer.WriteEndObject();
    }
}