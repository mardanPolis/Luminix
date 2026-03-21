using System;
using System.Collections.Generic;
using Verse.Identifiers;
using Verse.Collections;

namespace Verse.Rendering;

public static class RendererRegistry
{
    private struct RegistryEntry
    {
        public Renderer Renderer;
        public bool IsHot;
    } 

    private static Dictionary<Type, ID> registeredTypes = new Dictionary<Type, ID>();
    private static Dictionary<ID, RegistryEntry> registry = new Dictionary<ID, RegistryEntry>();

    private static Renderer[] allRenderers;
    private static Renderer[] hotRenderers;
    private static Renderer[] Renderers;

    public static void Register(Type rendererType, bool isHot = false)
    {
        if(!ValidRendererType(rendererType))
            return;


    }

    public static void Assign<T>(DenseList<T> list, Type rendererType)
    {
        if(!ValidRendererType(rendererType))
            return;

        if(!registeredTypes.TryGetValue(rendererType, out var iD))
            throw new ArgumentException($"Renderer type {rendererType.FullName} is not registered.");

        if(!registry.TryGetValue(iD, out var entry))
            throw new ArgumentException($"Renderer with ID {iD} is not registered.");

        var renderer = entry.Renderer;
        renderer.Assign(list);
    }

    private static bool ValidRendererType(Type rendererType)
    {
        if (!typeof(Renderer).IsAssignableFrom(rendererType))
            throw new ArgumentException($"Type {rendererType.FullName} does not inherit from Renderer.");

        return true;
    }

    public static Renderer[] GetRenderers()
    {
        return Renderers;
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
