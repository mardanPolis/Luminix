using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luminix;

public abstract class Def
{
    protected virtual void Initialize() {}

    public required string Name { get; init; }
}