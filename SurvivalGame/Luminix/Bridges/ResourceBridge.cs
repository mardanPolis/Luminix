using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Luminix.Bridges;

internal class ResourceBridge
{
    ConcurrentQueue<Resource> resources;
    ConcurrentQueue<Resource> resourcesRemoved;

    public void Read(Action<Resource, bool> action)
    {
        while(resources.TryDequeue(out Resource resource))
        {
            action(resource, true);
        }

        while(resourcesRemoved.TryDequeue(out Resource resource))
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
