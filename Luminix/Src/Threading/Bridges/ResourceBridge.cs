using System;
using System.Collections.Frozen;

namespace Luminix.Threading.Bridges;

internal class ResourceBridge
{
    private FrozenDictionary<(Type, string), Resource> resources = null;
    public void Set(FrozenDictionary<(Type, string), Resource> resources) => this.resources = resources;
    public FrozenDictionary<(Type, string), Resource> Get() => resources;
}
