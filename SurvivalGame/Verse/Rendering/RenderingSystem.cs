using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Verse.Rendering;

internal static class RenderingSystem
{
    private static FrameRenderer FrameRenderer;
    private static SpriteBatch spriteBatch;
    private static GraphicsDeviceManager graphics;


    private static RenderStack writeStack;
    private static volatile RenderStack currReadStack;
    private static RenderStack prevReadStack;

    private static volatile bool pendingStack = false;

    private static RenderStack readStack; // the stack currently used to draw frames
    private static RenderStack hotRenderStack; // the stack that is used for hot rendering (e.g. UI, debug, etc.)


    public static void Initialize(GraphicsDeviceManager graphics)
    {
        FrameRenderer = new FrameRenderer();

        writeStack = new RenderStack();
        currReadStack = new RenderStack();
        prevReadStack = new RenderStack();

        hotRenderStack = new RenderStack();

        RenderingSystem.graphics = graphics;
    }

    //CoreThread
    public static void Render(float maxTime, float time)
    {
        writeStack.Reset();
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(writeStack);

        if(maxTime * 0.9f > time) writeStack.Optimize();
        else writeStack.SkipOptimization();

        SwapReadStacks();
    }

    public static void RenderGizmos()
    {
        
    }

    private static void SwapReadStacks()
    {
        var temp = currReadStack;   // store currReadStack ref as temp
        currReadStack = writeStack; // currReadStack becomes writeStack
        writeStack = prevReadStack; // writeStack becomes prevReadStack
        prevReadStack = temp;       // prevReadStack becomes temp

        pendingStack = true;
    }

    //RenderThread
    public static void HotRender(float maxTime, float time)
    {
        hotRenderStack.Reset();

        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(hotRenderStack);
            
        if(maxTime * 0.9f > time) hotRenderStack.Optimize();
        else hotRenderStack.SkipOptimization();

    }

    public static void DrawFrame(float partialTick)
    {
        partialTick = Math.Clamp(partialTick, 0f, 1f);

        if(pendingStack && partialTick >= 1f)
        {
            readStack = currReadStack;
            pendingStack = false;
        }

        FrameRenderer.DrawFrame(hotRenderStack, readStack);
    }


    public static void SetSpriteBatch(SpriteBatch spriteBatch)
    {
        RenderingSystem.spriteBatch = spriteBatch;
        FrameRenderer.SetSpriteBatch(spriteBatch);
    }

}
