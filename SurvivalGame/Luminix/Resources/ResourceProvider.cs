using System;
using System.Collections.Generic;
using Luminix.Threading.Bridges;
using Luminix.Identifiers;
using Luminix.Workspaces;
using System.Diagnostics;
using System.Threading;

namespace Luminix.Resources;

public sealed class ResourceProvider : Workspace.Service
{
    internal ResourceProvider()
    {
        resources = new Dictionary<string, Resource>();
    }

    private Dictionary<string, Resource> resources;

    public T GetResource<T>(string assetName) where T : Resource
    {
        if (resources.TryGetValue(assetName, out Resource baseResource))
        {
            if (baseResource is T texture)
            {
                return texture;
            }

            throw new InvalidCastException($"Resource named '{assetName}' is a {baseResource.GetType().Name}, not a {typeof(T).Name}."
            );
        }

        throw new KeyNotFoundException($"Resource named '{assetName}' was not found.");
    }

    internal void AddResource(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource, nameof(resource));

        resources[resource.Name] = resource;
    }

    internal void UnloadResources()
    {
        resources.Clear();
    }
    internal void UnloadResource(string assetName)
    {
        resources.Remove(assetName);
    }
    internal void UnloadResources(string[] assetNames)
    {
        foreach (var assetName in assetNames)
        {
            UnloadResource(assetName);
        }
    }

    internal void WaitForResources(HashSet<string> resourceNames, ResourceBridge bridge)
    {
        while(resourceNames.Count > 0)
        {
            Resource item = bridge.TakeAppended();

            resources.Add(item.Name, item);
            
            resourceNames.Remove(item.Name);
        }
    }
}