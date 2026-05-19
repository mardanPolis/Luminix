using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Luminix.Threading.Bridges;

internal class ResourceBridge
{
    private BlockingCollection<Resource> resources;
    private BlockingCollection<Resource> resourcesRemoved;

    public BlockingCollection<Resource> AppendedResources => resources;
    public BlockingCollection<Resource> RemovedResources => resourcesRemoved;

    public void Read(Action<Resource, bool> action)
    {
        while(resources.TryTake(out Resource resource))
        {
            action(resource, true);
        }

        while(resourcesRemoved.TryTake(out Resource resource))
        {
            action(resource, false);
        }
    }

    public void Write(Resource resource, bool add)
    {
        if(add)
            resources.Append(resource);
        else
            resourcesRemoved.Append(resource);
    }
}
