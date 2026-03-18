using System;
using System.Collections;
using Verse.Identifiers;
using Verse.Rendering;

namespace Verse.Rendering;

internal static class RenderingSystem
{
    private static FrameRenderer FrameRenderer;

    private static RenderStack writeStack;
    private static RenderStack currReadStack;
    private static RenderStack prevReadStack;

    private static RenderStack hotRenderStack;

    public static void Initialize()
    {
        FrameRenderer = new FrameRenderer();

        writeStack = new RenderStack();
        currReadStack = new RenderStack();
        prevReadStack = new RenderStack();

        hotRenderStack = new RenderStack();
    }

    public static void Write()
    {
        writeStack.Clear();
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(writeStack);

        var temp = prevReadStack;
        prevReadStack = currReadStack;
        currReadStack = writeStack;
        writeStack = temp;
    }

    public static void BeginFrame()
    {
        hotRenderStack.Clear();
    }

    public static void DrawFrame()
    {
        foreach(var renderer in RendererRegistry.GetHotRenderers())
            renderer.RenderOn(hotRenderStack);

        var readStack = currReadStack;
        FrameRenderer.DrawFrame(hotRenderStack, readStack);
    }
}
