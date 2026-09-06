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
        Type converterType = TypeRegistry.GetClosedGenericType(typeof(Converter<>), typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    public class Converter<T> : JsonConverter<T> where T : class
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAbstract || typeToConvert.IsInterface;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.Null) return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            if(root.ValueKind != JsonValueKind.Object)
                return JsonSerializer.Deserialize<T>(root.GetRawText(), options);

            if(!TypeRegistry.TryGetDefaultSubtype(typeToConvert, out var targetType, out var forced) || !forced)
            {
                if (root.TryGetProperty("Type", out JsonElement typeProp) || root.TryGetProperty("type", out typeProp))
                {
                    string typeName = typeProp.GetString()!;
                    targetType = TypeRegistry.GetType(typeName);

                    if(!typeToConvert.IsAssignableFrom(targetType))
                        throw new InvalidOperationException($"Type '{targetType.FullName}' is not assignable to '{typeToConvert.FullName}'.");
                }
            }

            if(targetType == null)
                return null;

            if(TypeRegistry.IsOpenGenericType(targetType))
            {
                var genericArguments = TypeRegistry.GetGenericArgumentsClone(targetType);
                
                Type GetTypeArg(string key)
                {
                    if(!root.TryGetProperty(key, out var T) && !root.TryGetProperty(key.ToLower(), out T))
                        throw new JsonException($"Missing type-argument property '{key}'.");

                    string typeName = T.GetString()!;
                    var typeArg = TypeRegistry.GetType(typeName);
                    return typeArg;
                }

                if(genericArguments.Length > 1)
                {
                    for(int i = 0; i < genericArguments.Length; i++)
                    {
                        string key = $"Type{i + 1}";
                        genericArguments[i] = GetTypeArg(key);
                    }
                }
                else
                {
                    string key = $"Type";
                    genericArguments[0] = GetTypeArg(key);
                }

                targetType = TypeRegistry.GetClosedGenericType(targetType, genericArguments);
            }

            return (T)JsonSerializer.Deserialize(root.GetRawText(), targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {

            //FIXME
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
