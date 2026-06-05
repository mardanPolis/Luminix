using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Resources;

namespace Luminix.Rendering;

internal sealed class RenderingSystem : Workspace.Service
{
    private Main main;
    private FrameRenderer frameRenderer;
    private GraphicsDeviceManager graphics;
    private ResourceManager resourceManager;

    private RenderBuffer renderBuffer;

    private ulong frameCount = 0;
    private long drawCount = 0;

    public ResourceManager ResourceManager => resourceManager;
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
        renderBuffer = CoreToRender.RenderBridge.Read();

        frameRenderer = new FrameRenderer(main);
    }

    public void DrawFrame(float partialTick)
    {
        if (CoreToRender.RenderBridge.PendingStack)
        {
            renderBuffer = CoreToRender.RenderBridge.Read();
            renderBuffer.Sort();
        }

        partialTick = Math.Clamp(partialTick, 0f, 1f);
        frameRenderer.SetData(renderBuffer, partialTick, )

        if(debugRenderers != null)
            for (int i = 0; i < debugRenderers.Count; i++)
                debugRenderers[i].Render();

        Interlocked.Increment(ref frameCount);
        Interlocked.Exchange(ref drawCount, GraphicsDevice.Metrics.DrawCount);
    }
}
