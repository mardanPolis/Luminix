using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Verse.Rendering;

internal static class RenderingSystem
{
    private static FrameRenderer FrameRenderer;

    private static RenderStack writeStack;
    private static RenderStack currReadStack;
    private static RenderStack prevReadStack;

    private static RenderStack readStack; // the stack currently used to draw frames
    private static RenderStack hotRenderStack; // the stack that is used for hot rendering (e.g. UI, debug, etc.)

    private static SpriteBatch spriteBatch;
    private static GraphicsDeviceManager graphics;

    private static float partialTick = 0f;
    private static bool pendingStack = false;

    public static void Initialize(GraphicsDeviceManager graphics)
    {
        FrameRenderer = new FrameRenderer();

        writeStack = new RenderStack();
        currReadStack = new RenderStack();
        prevReadStack = new RenderStack();

        hotRenderStack = new RenderStack();

        RenderingSystem.graphics = graphics;
    }

    public static void BeginFrame(float partialTick)
    {
        hotRenderStack.Reset();

        RenderingSystem.partialTick = Math.Clamp(partialTick, 0f, 1f);

        if(pendingStack && RenderingSystem.partialTick >= 1f)
        {
            readStack = currReadStack;
            pendingStack = false;
        }
    }

    public static void Render()
    {
        writeStack.Reset();
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(writeStack);

        SwapReadStacks();
    }

    private static void SwapReadStacks()
    {
        var temp = prevReadStack;
        prevReadStack = currReadStack;
        currReadStack = temp;

        pendingStack = true;
    }

    public static void HotRender()
    {
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(hotRenderStack);

        int spriteCapacity = 0;
        int lineCapacity = 0;
        foreach(var renderer in RendererRegistry.GetHotRenderers())
        {
            spriteCapacity += renderer.GetSpriteRenderableCount();
            lineCapacity += renderer.GetLineRenderableCount();
        }

        hotRenderStack.Resize(spriteCapacity, lineCapacity);
    }

    public static void DrawFrame()
    {
        FrameRenderer.DrawFrame(hotRenderStack, readStack);
    }


    public static void SetSpriteBatch(SpriteBatch spriteBatch)
    {
        RenderingSystem.spriteBatch = spriteBatch;
        FrameRenderer.SetSpriteBatch(spriteBatch);
    }
}
