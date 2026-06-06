using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Resources;

namespace Luminix.Rendering;

public sealed class RenderingSystem : Workspace.Service
{
    internal RenderingSystem(Main main)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(main.GraphicsDeviceManager);

        this.main = main;
        this.graphics = main.GraphicsDeviceManager;
        this.resourceManager = main.ResourceManager;
    }

    private Main main;
    private BatchRenderer batchRenderer;
    private GraphicsDeviceManager graphics;
    private ResourceManager resourceManager;

    private SpriteBatch spriteBatch;

    private RenderBuffer renderBuffer;

    private ulong frameCount = 0;
    private long drawCount = 0;

    public ResourceManager ResourceManager => resourceManager;
    public GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public ulong FrameCount => Interlocked.Read(ref frameCount);
    public long DrawCount => Interlocked.Read(ref drawCount);

    internal SpriteBatch SpriteBatch => spriteBatch;

    internal void Initialize()
    {
        renderBuffer = CoreToRender.RenderBridge.Read();

        spriteBatch = new SpriteBatch(GraphicsDevice);
        batchRenderer = new BatchRenderer();
    }

    internal void DrawFrame(float partialTick)
    {
        if (CoreToRender.RenderBridge.PendingBuffer)
        {
            renderBuffer = CoreToRender.RenderBridge.Read();
            renderBuffer.Sort();
        }

        partialTick = Math.Clamp(partialTick, 0f, 1f);
        batchRenderer.SetData(renderBuffer, partialTick, graphics.GraphicsDevice.Viewport);
        batchRenderer.Render();

        Interlocked.Increment(ref frameCount);
        Interlocked.Exchange(ref drawCount, GraphicsDevice.Metrics.DrawCount);
    }
}
