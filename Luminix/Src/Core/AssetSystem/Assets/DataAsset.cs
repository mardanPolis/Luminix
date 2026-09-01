using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Luminix;

public sealed class DataAsset : Asset, IDisposable
{
    internal DataAsset(string identifier, JsonDocument json) : base(identifier)
    {
        Json = json;
    }

    public JsonDocument Json { get; }

    public void Dispose()
    {
        Json.Dispose();
    }
}
