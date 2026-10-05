using System.Text.Json;

namespace Luminix;

public sealed class DataAsset : Asset
{
    internal DataAsset(string identifier, JsonDocument json) : base(identifier)
    {
        Json = json;
    }

    public JsonDocument Json { get; }

    protected override void OnDispose()
    {
        Json.Dispose();
    }
}
