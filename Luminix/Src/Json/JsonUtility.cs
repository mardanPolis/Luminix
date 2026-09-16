using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Luminix.Json;

public static class JsonUtility
{
    public static readonly JsonSerializerOptions ReadSerializerOptions = new() {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = 
        {
            new TypedConverterFactory(),
            new ResourceConverterFactory(),
            new DefRefConverterFactory(),
            new VersionConverter(),
        }
    };

    public static readonly JsonSerializerOptions WriteSerializerOptions = new() {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        WriteIndented = true,
        Converters = 
        {
            new ResourceConverterFactory(),
            new DefRefConverterFactory(),
            new VersionConverter(),
        }
    };

    public static void MergeJsonObjects(JsonObject parent, JsonObject child)
    {
        foreach (var (key, parentNode) in parent)
        {
            if (child.TryGetPropertyValue(key, out var childNode) && childNode is not null)
            {
                if (parentNode is JsonObject parentChildObj && childNode is JsonObject childChildObj)
                    MergeJsonObjects(parentChildObj, childChildObj);
            }
            else
            {
                child[key] = parentNode?.DeepClone();
            }
        }
    }

    public static JsonElement MergeJsonElements(JsonElement parent, JsonElement child)
    {
        var parentObj = JsonObject.Create(parent)!;
        var childObj = JsonObject.Create(child)!;

        MergeJsonObjects(parentObj, childObj);

        return JsonSerializer.SerializeToElement(childObj);
    }


    public static JsonObject Parse(Dictionary<string, JsonElement> elements)
    {
        return new JsonObject(elements.Select(kvp => new KeyValuePair<string, JsonNode>(kvp.Key, JsonNode.Parse(kvp.Value.GetRawText()))));
    }


    public static void Serialize<T>(FileStream fileStream, T data)
    {
        JsonSerializer.Serialize<T>(fileStream, data, WriteSerializerOptions);
    }

    public static Task SerializeAsync<T>(FileStream fileStream, T data)
    {
        return JsonSerializer.SerializeAsync<T>(fileStream, data, WriteSerializerOptions);
    }


    public static T Deserialize<T>(JsonElement element)
    {
        return JsonSerializer.Deserialize<T>(element, ReadSerializerOptions);
    }

    public static T Deserialize<T>(JsonNode node)
    {
        return JsonSerializer.Deserialize<T>(node, ReadSerializerOptions);
    }

    public static T Deserialize<T>(JsonDocument node)
    {
        return JsonSerializer.Deserialize<T>(node, ReadSerializerOptions);
    }

    public static T Deserialize<T>(string node)
    {
        return JsonSerializer.Deserialize<T>(node, ReadSerializerOptions);
    }
    
    public static object Deserialize(string node, Type baseType)
    {
        return JsonSerializer.Deserialize(node, baseType, ReadSerializerOptions);
    }
    
    public static object Deserialize(JsonElement element, Type baseType)
    {
        return JsonSerializer.Deserialize(element, baseType, ReadSerializerOptions);
    }

    public static object Deserialize(JsonNode node, Type baseType)
    {
        return JsonSerializer.Deserialize(node, baseType, ReadSerializerOptions);
    }

    public static object Deserialize(JsonDocument node, Type baseType)
    {
        return JsonSerializer.Deserialize(node, baseType, ReadSerializerOptions);
    }
}