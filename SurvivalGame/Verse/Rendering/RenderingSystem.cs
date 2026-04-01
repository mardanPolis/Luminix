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

        int[] interpSpritePrevCount = new int[RenderStack.OPTIMIZATION_DELAY_TICKS];
        int[] spritePrevCount = new int[RenderStack.OPTIMIZATION_DELAY_TICKS];
        int[] linePrevCount = new int[RenderStack.OPTIMIZATION_DELAY_TICKS];

        writeStack = new RenderStack(interpSpritePrevCount, spritePrevCount, linePrevCount);
        currReadStack = new RenderStack(interpSpritePrevCount, spritePrevCount, linePrevCount);
        prevReadStack = new RenderStack(interpSpritePrevCount, spritePrevCount, linePrevCount);

        readStack = currReadStack;

        int[] interpSpritePrevCountHot = new int[RenderStack.OPTIMIZATION_DELAY_TICKS];
        int[] spritePrevCountHot = new int[RenderStack.OPTIMIZATION_DELAY_TICKS];
        int[] linePrevCountHot = new int[RenderStack.OPTIMIZATION_DELAY_TICKS];

        hotRenderStack = new RenderStack(interpSpritePrevCountHot, spritePrevCountHot, linePrevCountHot);

        RenderingSystem.graphics = graphics;
    }

    //CoreThread
    public static void Render()
    {
        writeStack.Reset();
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(writeStack);
    }

    public static void DebugRender()
    {
        writeStack.Reset();
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(writeStack);

        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderGizmosOn(writeStack);
    }

    public static void SwapRenderStacks(float maxTime, float time)
    {
        if(maxTime * 0.9f > time) writeStack.Optimize();
        else writeStack.SkipOptimization();

        var temp = currReadStack;   // store currReadStack ref as temp
        currReadStack = writeStack; // currReadStack becomes writeStack
        writeStack = prevReadStack; // writeStack becomes prevReadStack
        prevReadStack = temp;       // prevReadStack becomes temp

        pendingStack = true;
    }

    //RenderThread
    public static void HotRender(double maxTime, double time)
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
