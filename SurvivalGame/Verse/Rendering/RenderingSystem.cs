using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Bridges;
using Verse.Rendering.Renderers;

namespace Verse.Rendering;

internal static class RenderingSystem
{
    private static FrameRenderer frameRenderer;
    private static DebugRenderer debugRenderer;
    private static GraphicsDeviceManager graphics;

    private static RenderStack readStack; // the stack currently used to draw frames

    private static ulong frameCount = 0;
    private static long drawCount = 0;

    static float prevPartialTick = 0;

    public static GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public static GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public static ulong FrameCount => Interlocked.Read(ref frameCount);
    public static long DrawCount => Interlocked.Read(ref drawCount);

    public static void Initialize(GraphicsDeviceManager graphics)
    {
        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
        graphics.IsFullScreen = false;
        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.ApplyChanges();

        readStack = CoreToRender.Read();

        RenderingSystem.graphics = graphics;
    }

    public static void InitializeRenderers(bool isDebug)
    {
        var spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
        frameRenderer = new FrameRenderer(spriteBatch);
        if (isDebug) debugRenderer = new DebugRenderer(spriteBatch);
    }

    public static void DrawFrame(float partialTick)
    {
        if (CoreToRender.PendingStack)
            readStack = CoreToRender.Read();

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        frameRenderer.DrawFrame(readStack, graphics.GraphicsDevice.Viewport, partialTick);

        if(debugRenderer is not null) debugRenderer.Render();
        
        Interlocked.Increment(ref frameCount);
        Interlocked.Exchange(ref drawCount, GraphicsDevice.Metrics.DrawCount);

        prevPartialTick = partialTick;
    }

    public static DebugRenderer DebugRenderer => debugRenderer;
}
