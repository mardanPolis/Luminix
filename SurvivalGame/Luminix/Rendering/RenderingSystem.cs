using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Bridges;

namespace Luminix.Rendering;

internal sealed class RenderingSystem
{
    private Main main;
    private FrameRenderer frameRenderer;
    private GraphicsDeviceManager graphics;

    private RenderBuffer readStack; // the stack currently used to draw frames

    private ulong frameCount = 0;
    private long drawCount = 0;

    public GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public ulong FrameCount => Interlocked.Read(ref frameCount);
    public long DrawCount => Interlocked.Read(ref drawCount);

    private List<DebugRenderer> debugRenderers;

    public RenderingSystem(Main main)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(main.GraphicsDeviceManager);

        this.main = main;
        this.graphics = main.GraphicsDeviceManager;
    }

    public void Initialize()
    {
        readStack = CoreToRender.RenderBridge.Read();

        var spriteBatch = new SpriteBatch(graphics.GraphicsDevice, 1024);
        frameRenderer = new FrameRenderer(spriteBatch, main);

        if (Settings.Debug)
        {
            
        }
    }

    public void DrawFrame(float partialTick)
    {
        if (CoreToRender.RenderBridge.PendingStack)
        {
            readStack = CoreToRender.RenderBridge.Read();
            readStack.Sort();
        }

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        frameRenderer.RenderFrame(readStack, graphics.GraphicsDevice.Viewport, partialTick);

        if(debugRenderers != null)
            for (int i = 0; i < debugRenderers.Count; i++)
                debugRenderers[i].Render();

        Interlocked.Increment(ref frameCount);
        Interlocked.Exchange(ref drawCount, GraphicsDevice.Metrics.DrawCount);
    }

    public void ToggleVSync(bool? state = null)
    {
        if (graphics == null) return;

        graphics.SynchronizeWithVerticalRetrace = state ?? !graphics.SynchronizeWithVerticalRetrace;
    }

    public void ApplyChanges()
    {
        if(graphics.SynchronizeWithVerticalRetrace)
        {
            main.IsFixedTimeStep = false;
        }

        graphics.ApplyChanges();
    }
}
