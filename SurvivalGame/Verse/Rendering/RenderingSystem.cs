using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Bridges;

namespace Verse.Rendering;

internal static class RenderingSystem
{
    private static FrameRenderer FrameRenderer;
    private static GraphicsDeviceManager graphics;

    private static RenderStack readStack; // the stack currently used to draw frames

    private static ulong frameCount = 0;
    private static long drawCount = 0;

    static float prevPartialTick = 0;

    public static SpriteBatch SpriteBatch { get; private set; }

    public static GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public static GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public static ulong FrameCount => frameCount;
    public static long DrawCount => drawCount;

    public static void Initialize(GraphicsDeviceManager graphics, bool isDebug)
    {
        FrameRenderer = new FrameRenderer(new SpriteBatch(graphics.GraphicsDevice));

        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
        graphics.IsFullScreen = false;
        graphics.SynchronizeWithVerticalRetrace = true;
        graphics.ApplyChanges();

        readStack = CoreToRender.Read();

        RenderingSystem.graphics = graphics;
    }

    public static void DrawFrame(float partialTick)
    {
        if (CoreToRender.PendingStack && partialTick <= prevPartialTick)
            readStack = CoreToRender.Read();

        FrameRenderer.DrawFrame(readStack, graphics.GraphicsDevice.Viewport, partialTick);
        frameCount++;

        drawCount = GraphicsDevice.Metrics.DrawCount;

        prevPartialTick = partialTick;
    }
}
