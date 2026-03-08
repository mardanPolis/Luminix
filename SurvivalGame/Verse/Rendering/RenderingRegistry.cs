using System;
using System.Collections.Generic;
using System.Net;
using Verse.Identifiers;
using Verse.Rendering;

namespace Verse;

public static class RenderingRegistry
{
    private static readonly List<Type> registeredRenderers = new();
    private static readonly Dictionary<Type, Dictionary<Variant, Type>> variantTypeRenderer = new();
    private static readonly Dictionary<Type, Type> typeRenderer = new();

    public static void Register(Type objectType, Type rendererType)
    {
        //Checking object type
        if (!objectType.IsClass)
            throw new ArgumentException($"{objectType} must be a class.");

        if (!typeof(IRenderable<>).IsAssignableFrom(objectType))
            throw new ArgumentException($"{objectType} must implement IRenderable.");
        

        //Checking renderer type
        if (!rendererType.IsClass || !typeof(Renderer<>).IsAssignableFrom(rendererType))
            throw new ArgumentException($"{rendererType} must be a subclass of Renderer<TRenderData>.");

    }


    public static IReadOnlyList<Type> Registered => registeredRenderers.AsReadOnly();
}
