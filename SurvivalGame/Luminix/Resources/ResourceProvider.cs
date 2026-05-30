using System;
using System.Collections;
using System.Collections.Generic;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;

namespace Luminix.Resources;

public sealed class ResourceProvider : Workspace.Service, IEnumerable<Resource>
{
    internal ResourceProvider() { }
 
    private IReadOnlyDictionary<string, Resource> resources;

    public T GetResource<T>(string resourceName) where T : Resource
    {
        if (resources != null && resources.TryGetValue(resourceName, out Resource baseResource))
        {
            if (baseResource is T resource)
            {
                return resource;
            }
            
            throw new InvalidCastException($"Resource named '{resourceName}' is a {baseResource.GetType().Name}, not a {typeof(T).Name}."
            );
        }

        throw new KeyNotFoundException($"Resource named '{resourceName}' was not found.");
    }

    public int ResourceCount => resources != null ? resources.Count : 0;

    internal void Set(IReadOnlyDictionary<string, Resource> resources)
    {
        this.resources = resources;
    }

    public IEnumerator<Resource> GetEnumerator()
    {
        return resources.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}