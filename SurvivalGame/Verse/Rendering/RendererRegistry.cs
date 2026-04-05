using System;
using System.Collections.Generic;
using Verse.Identifiers;
using Verse.Collections;

namespace Verse.Rendering;

public static class RendererRegistry
{
    private readonly struct RegistryEntry(Renderer renderer, Type renderableType)
    {
        public readonly Renderer Renderer = renderer;
        public readonly Type RenderableType = renderableType;
    } 

    private static Dictionary<Type, ID> registeredTypes = new Dictionary<Type, ID>();
    private static Dictionary<ID, RegistryEntry> registry = new Dictionary<ID, RegistryEntry>();

    private static Renderer[] renderers;

    public static void Register(Type rendererType)
    {
        bool InheritsRenderer(Type type)
        {
            Type current = type.BaseType;
            while (current != null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Renderer<>))
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        if(!InheritsRenderer(rendererType))
            throw new ArgumentException($"Type {rendererType.FullName} must inherit from Renderer<T>.");

        Renderer renderer = (Renderer)Activator.CreateInstance(rendererType);
        ID id = new ID((uint)registeredTypes.Count);
        RegistryEntry registryEntry = new (renderer, renderer.RenderableType);
        registeredTypes.TryAdd(rendererType, id);
        registry.TryAdd(id, registryEntry);
    }

    public static void Assign<T>(DenseList<T> list, Type rendererType)
    {
        if(!registeredTypes.TryGetValue(rendererType, out var iD))
            throw new ArgumentException($"Renderer type {rendererType.FullName} is not registered.");

        if(!registry.TryGetValue(iD, out var entry))
            throw new ArgumentException($"Renderer with ID {iD} is not registered.");

        var renderer = entry.Renderer;
        renderer.Assign(list);
    }


    internal static void FinalizeRegistry()
    {
        List<Renderer> renderers = new List<Renderer>();

        foreach(var entry in registry.Values)
        {
            var renderer = entry.Renderer ?? throw new NullReferenceException("Null Renderer has been registered!");
            renderers.Add(renderer);
        }

        RendererRegistry.renderers = renderers.ToArray();

        foreach(var renderer in renderers)
            renderer.Optimize();
    }

    public static Renderer[] GetRenderers()
    {
        return renderers;
    }
}
