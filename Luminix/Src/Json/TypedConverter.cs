using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Luminix.Json;

public class TypedConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsAbstract || typeToConvert.IsInterface;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(TypedConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    public class TypedConverter<T> : JsonConverter<T> where T : class
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.Null) return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            if(root.ValueKind != JsonValueKind.Object)
                return JsonSerializer.Deserialize<T>(root.GetRawText(), options);

            bool isAbstractOrInterface = typeof(T).IsAbstract || typeof(T).IsInterface;
            bool hasTypeProperty = root.TryGetProperty("Type", out JsonElement typeProp) || root.TryGetProperty("type", out typeProp);

            Type targetType = typeof(T);

            if (hasTypeProperty)
            {
                string typeName = typeProp.GetString()!;
                targetType = TypeRegistry.GetType(typeName) ?? throw new JsonException($"Could not resolve type for string: '{typeName}'");
            }
            else if (isAbstractOrInterface)
            {
                throw new JsonException($"Cannot instantiate abstract type '{typeof(T).Name}' without a 'Type' discriminator.");
            }

            return (T)JsonSerializer.Deserialize(root.GetRawText(), targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            Type actualType = value.GetType();

            JsonNode node = JsonSerializer.SerializeToNode(value, actualType, options)!;

            if (node is JsonObject jsonObject)
            {
                if (!jsonObject.ContainsKey("Type") && !jsonObject.ContainsKey("type"))
                {
                    string typeName = TypeRegistry.GetName(actualType);

                    if (!string.IsNullOrEmpty(typeName))
                    {
                        jsonObject.Insert(0, "Type", JsonValue.Create(typeName));
                    }
                }

                jsonObject.WriteTo(writer, options);
            }
            else
            {
                JsonSerializer.Serialize(writer, value, actualType, options);
            }
        }
    }
}
