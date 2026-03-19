using System;
using System.Collections;
using Verse.Identifiers;
using Verse.Rendering;

namespace Verse.Rendering;

public static class RenderingSystem
{
    private static UPSCounter upsCounter;
    private static FrameRenderer FrameRenderer;

    private static RenderStack writeStack;
    private static RenderStack currReadStack;
    private static RenderStack prevReadStack;

    private static RenderStack hotRenderStack;

    private static RenderStack readStack;

    private static float partialTick = 0f;
    private static bool pendingStack = false;

    internal static void Initialize()
    {
        FrameRenderer = new FrameRenderer();

        writeStack = new RenderStack();
        currReadStack = new RenderStack();
        prevReadStack = new RenderStack();

        hotRenderStack = new RenderStack();
    }

    internal static void BeginFrame()
    {
        hotRenderStack.Clear();

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        if(pendingStack && partialTick >= 1f)
        {
            readStack = currReadStack;
            pendingStack = false;
        }
    }

    internal static void Render()
    {
        writeStack.Clear();
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(writeStack);

        var temp = prevReadStack;
        prevReadStack = currReadStack;
        currReadStack = writeStack;
        writeStack = temp;

        pendingStack = true;
    }

    internal static void HotRender()
    {
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(hotRenderStack);
    }

    internal static void DrawFrame()
    {
        foreach(var renderer in RendererRegistry.GetHotRenderers())
            renderer.RenderOn(hotRenderStack);

        FrameRenderer.DrawFrame(hotRenderStack, readStack);
    }

    public static bool CountFPS { 
        get { return upsCounter != null; } 
        set
        {
            if(value && upsCounter == null)
                upsCounter = new UPSCounter();
            else if(!value && upsCounter != null)
                upsCounter = null;
        }
     }
}
