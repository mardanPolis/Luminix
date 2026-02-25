using System;
using System.Collections.Generic;
using SurvivalGame;
using SurvivalGame.Engine.Rendering;

public static class RendererRegistry
{
    private static readonly Dictionary<Type, IRenderer> registeredRenderers = new();
    private static readonly List<IRenderer> renderers = new();

    public static void Register(Type rendererType)
    {
        if (!typeof(IRenderer).IsAssignableFrom(rendererType))
            throw new ArgumentException($"Renderer type {rendererType} does not implement IRenderer.");

        if (registeredRenderers.ContainsKey(rendererType))
        {
            Main.LOGGER.Wrn($"Renderer {rendererType} already registered.");
            return;
        }

        var renderer = (IRenderer)Activator.CreateInstance(rendererType);
        renderers.Add(renderer);
        registeredRenderers.Add(rendererType, renderer);
    }

    public static T GetRenderer<T>() where T : IRenderer
    {
        if (registeredRenderers.TryGetValue(typeof(T), out var renderer))
            return (T)renderer;

        throw new KeyNotFoundException($"Renderer of type {typeof(T)} is not registered.");
    }

    internal static void UpdateData()
    {
        foreach (var renderer in renderers)
            renderer.UpdateData();
    }

    internal static void RenderData(float partialTicks)
    {
        foreach (var renderer in renderers)
            renderer.RenderData(partialTicks);
    }
}