using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Luminix.Threading.Bridges;
using Luminix.Identifiers;
using Luminix.Workspaces;

namespace Luminix;

public sealed class Resources : Workspace.Service
{
    internal Resources()
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
            Resource item = bridge.AppendedResources.Take();

            resources.Add(item.Name, item);
            
            resourceNames.Remove(item.Name);
        }
    }

    internal void WaitForResources(HashSet<string> resourceNames, ResourceBridge bridge, Action<string, int, int> onReceive)
    {
        int resourceEndingCount = resourceNames.Count;

        while(resourceNames.Count > 0)
        {
            Resource item = bridge.AppendedResources.Take();
            
            resources.Add(item.Name, item);
            
            resourceNames.Remove(item.Name);

            onReceive(item.Name, resourceEndingCount - resourceNames.Count, resourceEndingCount);
        }
    }
}


public abstract class Resource
{
    public readonly string Name;
    internal readonly ID ID;
    internal Resource(string name, ID id)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        Name = name;
        ID = id;
    }
}

public class TextureResource : Resource
{
    public readonly int Height;
    public readonly int Width;

    internal TextureResource(string name, ID id, int width, int height) 
        : base(name, id)
    {
        Width = width;
        Height = height;
    }
}

public class FontResource : Resource
{
    internal FontResource(string name, ID id)
        : base(name, id) {}
}

public class ShaderResource : Resource
{
    internal ShaderResource(string name, ID id)
        : base(name, id) {}
}

public class AudioResource : Resource
{
    public readonly double Duration;
    internal AudioResource(string name, ID id, double duration) 
        : base(name, id)
    {
        Duration = duration;
    }
}