using System.Text.Json.Serialization;

namespace Luminix.JsonContext;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(EntityType))]
[JsonSerializable(typeof(DefNode))]
internal partial class LuminixJsonContext : JsonSerializerContext
{
}