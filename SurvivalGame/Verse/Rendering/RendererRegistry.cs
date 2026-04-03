using System;
using System.Collections.Generic;
using Verse.Identifiers;
using Verse.Collections;

namespace Verse.Rendering;

public static class RendererRegistry
{
    private readonly struct RegistryEntry(Renderer renderer, Type renderableType, bool isHot)
    {
        public readonly Renderer Renderer = renderer;
        public readonly Type RenderableType = renderableType;
        public readonly bool IsHot = isHot;
    } 

    private static Dictionary<Type, ID> registeredTypes = new Dictionary<Type, ID>();
    private static Dictionary<ID, RegistryEntry> registry = new Dictionary<ID, RegistryEntry>();

    private static Renderer[] allRenderers;
    private static Renderer[] hotRenderers;
    private static Renderer[] renderers;

    public static void Register(Type rendererType, bool isHot = false)
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
        RegistryEntry registryEntry = new (renderer, renderer.RenderableType, isHot);
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
        List<Renderer> allRenderers = new List<Renderer>();
        List<Renderer> hotRenderers = new List<Renderer>();
        List<Renderer> renderers = new List<Renderer>();

        foreach(var entry in registry.Values)
        {
            var renderer = entry.Renderer;
            var isHot = entry.IsHot;

            if(renderer is null)
                throw new NullReferenceException("Null Renderer has been registered!");

            allRenderers.Add(renderer);
            if(isHot)
                hotRenderers.Add(renderer);
            else
                renderers.Add(renderer);
        }

        RendererRegistry.allRenderers = allRenderers.ToArray();
        RendererRegistry.hotRenderers = hotRenderers.ToArray();
        RendererRegistry.renderers = renderers.ToArray();

        foreach(var renderer in allRenderers)
            renderer.Optimize();
    }

    public static Renderer[] GetRenderers()
    {
        return renderers;
    }

    public static Renderer[] GetHotRenderers()
    {
        return hotRenderers;
    }

    public static Renderer[] GetAllRenderers()
    {
        return allRenderers;
    }
}
