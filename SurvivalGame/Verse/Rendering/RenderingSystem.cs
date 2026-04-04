using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Simulation;

namespace Verse.Rendering;

internal static class RenderingSystem
{
    private static IFrameRenderer FrameRenderer;
    private static DebugFrameRenderer DebugFrameRenderer;
    private static GraphicsDeviceManager graphics;
    public static SpriteBatch SpriteBatch { get; private set; }


    private static RenderStack writeStack;
    private static volatile RenderStack currReadStack;
    private static RenderStack prevReadStack;

    private static volatile bool pendingStack = false;

    private static RenderStack readStack; // the stack currently used to draw frames
    private static RenderStack hotRenderStack; // the stack that is used for hot rendering (e.g. UI, debug, etc.)

    private static ulong frameCount = 0;

    public static void Initialize(GraphicsDeviceManager graphics, bool isDebug)
    {
        if (isDebug)
        {
            DebugFrameRenderer = new DebugFrameRenderer(new SpriteBatch(graphics.GraphicsDevice));
            FrameRenderer = DebugFrameRenderer;
        }
        else
            FrameRenderer = new FrameRenderer(new SpriteBatch(graphics.GraphicsDevice));

        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.ApplyChanges();

        RenderStack.StackSharedData sharedDataInterpSprite = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataSprite = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataLine = new RenderStack.StackSharedData();

        writeStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine);
        currReadStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine);
        prevReadStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine);

        readStack = currReadStack;

        RenderStack.StackSharedData sharedDataInterpSpriteHot = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataSpriteHot = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataLineHot = new RenderStack.StackSharedData();

        hotRenderStack = new RenderStack(sharedDataInterpSpriteHot, sharedDataSpriteHot, sharedDataLineHot);

        RenderingSystem.graphics = graphics;
    }

    //CoreThread
    public static void Render()
    {
        writeStack.Reset();
        
        writeStack.PostCameraData(Camera.MainCamera);
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(writeStack);
    }

    public static void DebugRender()
    {
        writeStack.Reset();
        
        writeStack.PostCameraData(Camera.MainCamera);

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
        if (pendingStack)
        {
            if(readStack != currReadStack)
                partialTick -= (int)partialTick;
            readStack = currReadStack;
            pendingStack = false;
        }

        FrameRenderer.DrawFrame(hotRenderStack, readStack, partialTick);
        frameCount++;
    }

    public static GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public static GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public static ulong FrameCount => frameCount;
    public static ulong DrawCalls
    {
        get
        {
            if(DebugFrameRenderer != null)
                return DebugFrameRenderer.DrawCalls;
            else
                return 0;
        }
    }
}
