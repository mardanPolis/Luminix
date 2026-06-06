using System.Collections.Generic;
using Luminix.Resources;

namespace Luminix.Threading.Bridges;

internal class ResourceBridge
{
    private IReadOnlyDictionary<string, Resource> resources = null;
    public void Set(IReadOnlyDictionary<string, Resource> resources) => this.resources = resources;
    public IReadOnlyDictionary<string, Resource> Get() => resources;
}
