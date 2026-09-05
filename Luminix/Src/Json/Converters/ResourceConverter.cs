using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luminix.Json;

public class ResourceConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return !typeToConvert.IsAbstract && typeof(Resource).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = TypeRegistry.GetClosedGenericType(typeof(Converter<>), typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    public class Converter<T> : JsonConverter<T> where T : Resource
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(T);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            string path = reader.GetString();
            return path == null ? null : Workspace.Current.GetService<Resources>().GetResource<T>(path);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.Name);
        }
    }
}
