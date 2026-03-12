using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Verse.Identifiers;
using Verse.Collections;
using System.Linq;

namespace Verse.Rendering;

public static class RenderingRegistry
{
    private struct RegistryEntry(Type rendererType, Type renderDataType, ICollection renderables)
    {
        public Type RendererType { get; private set; } = rendererType;
        public Type RenderDataType { get; private set; } = renderDataType;
        public ICollection Renderables { get; private set; } = renderables;
    }
    private static List<RegistryEntry> entries = new List<RegistryEntry>();

    private static ID lastRendererID = new(0);
    private static Dictionary<Type, ID> registeredRendererTypes = null;
    private static Dictionary<ID, RenderDataContainer[][]> rendererRenderDataContainers = null;
    private static Renderer[] renderers = null;

    public static void Register<TRenderData>(DenseList<IRenderable<TRenderData>> renderableList, Type rendererType) where TRenderData : struct
    {
        //checks if registry is not finalized
        if(renderers != null || entries == null)
            throw new InvalidOperationException("Cannot register new renderer object array after the registry has been finalized.");

        Type renderDataType = typeof(TRenderData);

        //check if the given renderer type is a subclass of Renderer<TRenderData>.
        if(!typeof(Renderer<TRenderData>).IsAssignableFrom(rendererType))
            throw new ArgumentException("Renderer type must be a subclass of Renderer.", nameof(rendererType));

        entries.Add(new RegistryEntry(rendererType, renderDataType, renderableList));
    }

    public static bool TryGet(Type renderDataType, out Renderer renderer)
    {
        renderer = null;
        if (registeredRendererTypes.TryGetValue(renderDataType, out ID rendererId))
        {
            renderer = renderers[rendererId.Value];
            return true;
        }

        return false;
    }
    public static bool TryGet(uint rendererId, out Renderer renderer)
    {
        renderer = null;
        if (rendererId < renderers.Length)
        {
            renderer = renderers[rendererId];
            return true;
        }

        return false;
    }

    public static void Visit(Action<Renderer> visitor)
    {
        foreach (var renderer in renderers)
            visitor(renderer);
    }
    internal static void Visit(ID rendererID, Action<RenderDataContainer[]> visitor)
    {
        if (rendererRenderDataContainers.TryGetValue(rendererID, out RenderDataContainer[][] renderDataContainerSets))
        {
            foreach (var renderDataContainers in renderDataContainerSets)
                visitor(renderDataContainers);
        }
    }

    public static void FinalizeRegistry()
    {
        //registering renderer type and getting its ID
        var registeredRendererTypes = new Dictionary<Type, ID>();

        foreach(var entry in entries)
        {
            if(!registeredRendererTypes.TryGetValue(entry.RendererType, out ID rendererId))
            {
                registeredRendererTypes[entry.RendererType] = lastRendererID;
                lastRendererID = new(lastRendererID.Value + 1);
            }
        }

        //creating RenderDataContainers for renderers
        var rendererRenderDataContainers = new Dictionary<ID, List<RenderDataContainer[]>>();

        foreach(var entry in entries)
        {
            if(!registeredRendererTypes.TryGetValue(entry.RendererType, out ID rendererId))
                throw new InvalidOperationException($"Renderer type {entry.RendererType.Name} was not registered.");

            Type renderDataContainerType = typeof(RenderDataContainer<>).MakeGenericType(entry.RenderDataType);
            RenderDataContainer[] renderDataContainers = [
                (RenderDataContainer)Activator.CreateInstance(renderDataContainerType, rendererId, entry.Renderables),
                (RenderDataContainer)Activator.CreateInstance(renderDataContainerType, rendererId, entry.Renderables),
                (RenderDataContainer)Activator.CreateInstance(renderDataContainerType, rendererId, entry.Renderables),
            ];

            rendererRenderDataContainers.TryAdd(rendererId, new List<RenderDataContainer[]>());
            rendererRenderDataContainers[rendererId].Add(renderDataContainers);
        }

        //creating renderer instance
        var renderers = new Renderer[registeredRendererTypes.Count];
        
        foreach(var kvp in registeredRendererTypes)
        {
            Type rendererType = kvp.Key;
            ID rendererId = kvp.Value;

            Renderer rendererInstance = (Renderer)Activator.CreateInstance(rendererType);
            rendererInstance.Assign(rendererId);
            renderers[rendererId.Value] = rendererInstance;
        }

        RenderingRegistry.registeredRendererTypes = registeredRendererTypes;
        RenderingRegistry.rendererRenderDataContainers = rendererRenderDataContainers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        RenderingRegistry.renderers = renderers;
        entries = null;
    }

    public static ImmutableArray<Type> Registered => registeredRendererTypes.Keys.ToImmutableArray();
}
