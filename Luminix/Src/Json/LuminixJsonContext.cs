using System.Text.Json.Serialization;

namespace Luminix.Json;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(EntityType))]
[JsonSerializable(typeof(DefNode))]
internal partial class LuminixJsonContext : JsonSerializerContext
{
}