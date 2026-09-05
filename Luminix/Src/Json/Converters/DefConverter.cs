using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luminix.Json;

public class DefRefConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(DefRef<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type defType = typeToConvert.GetGenericArguments()[0];
        Type converterType = TypeRegistry.GetClosedGenericType(typeof(Converter<>), defType);
        return (JsonConverter)Activator.CreateInstance(converterType);
    }

    public class Converter<T> : JsonConverter<DefRef<T>> where T : Def
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType &&
                typeToConvert.GetGenericTypeDefinition() == typeof(DefRef<>);
        }

        public override DefRef<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected string for DefRef<{typeof(T).Name}>, got {reader.TokenType}.");

            string name = reader.GetString();
            return new DefRef<T>(name);
        }

        public override void Write(Utf8JsonWriter writer, DefRef<T> value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Name);
        }
    }
}