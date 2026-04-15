using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Bridges;

namespace Verse.Rendering;

internal static class RenderingSystem
{
    private static Main main;
    private static FrameRenderer frameRenderer;
    private static GraphicsDeviceManager graphics;

    private static RenderStack readStack; // the stack currently used to draw frames

    private static ulong frameCount = 0;
    private static long drawCount = 0;

    public static GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public static GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public static ulong FrameCount => Interlocked.Read(ref frameCount);
    public static long DrawCount => Interlocked.Read(ref drawCount);

    private static List<DebugRenderer> debugRenderers;

    public static void Initialize(Main main, GraphicsDeviceManager graphics)
    {
        RenderingSystem.main = main;
        RenderingSystem.graphics = graphics;

        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
        graphics.IsFullScreen = false;
        ToggleVSync(false);
        ApplyChanges();

        readStack = CoreToRender.Read();
    }

    public static void InitializeRenderers(bool isDebug)
    {
        var spriteBatch = new SpriteBatch(graphics.GraphicsDevice, 10000);
        frameRenderer = new FrameRenderer(spriteBatch, graphics.GraphicsDevice);

        if (isDebug)
        {
            
        }
    }

    public static void DrawFrame(float partialTick)
    {
        if (CoreToRender.PendingStack)
            readStack = CoreToRender.Read();

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        frameRenderer.RenderFrame(readStack, graphics.GraphicsDevice.Viewport, partialTick);

        if(debugRenderers != null)
            for (int i = 0; i < debugRenderers.Count; i++)
                debugRenderers[i].Render();

        Interlocked.Increment(ref frameCount);
        Interlocked.Exchange(ref drawCount, GraphicsDevice.Metrics.DrawCount);
    }

    public static void ToggleVSync(bool? state = null)
    {
        if (graphics == null) return;

        graphics.SynchronizeWithVerticalRetrace = state ?? !graphics.SynchronizeWithVerticalRetrace;
        graphics.ApplyChanges();
    }

    public static void ApplyChanges()
    {
        if(graphics.SynchronizeWithVerticalRetrace)
        {
            main.IsFixedTimeStep = false;
        }

        graphics.ApplyChanges();
    }
}
