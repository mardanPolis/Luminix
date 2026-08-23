using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luminix;

public sealed class DefNode
{
    public required string DefType { get; init; }
    public required string DefName { get; init; }
    public bool Abstract { get; }
    public string Parent { get; }
    [JsonExtensionData]
    public required Dictionary<string, JsonElement> Properties { get; init; }
}