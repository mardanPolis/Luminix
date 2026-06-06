using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Resources;
using Luminix.Simulation;
using System.Collections.Generic;

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
    private SystemDebugRenderer systemDebugRenderer;
    private GraphicsDeviceManager graphics;
    private ResourceManager resourceManager;

    private SpriteBatch spriteBatch;

    private RenderBuffer renderBuffer;

    private ulong frameCount = 0;
    private long drawCount = 0;

    private Viewport viewport;

    private Matrix viewMatrix;
    private Matrix projectionMatrix;

    public ResourceManager ResourceManager => resourceManager;
    public GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public ulong FrameCount => Interlocked.Read(ref frameCount);
    public long DrawCount => Interlocked.Read(ref drawCount);
    public Viewport Viewport => viewport;

    public Matrix ViewMatrix => viewMatrix;
    public Matrix ProjectionMatrix => projectionMatrix;
    public Matrix MatrixTransform => viewMatrix * projectionMatrix;

    internal SpriteBatch SpriteBatch => spriteBatch;

    //TODO : add a way to create ImmediateRenderers

    List<ImmediateRenderer> immediateRenderers;

    internal void Initialize()
    {
        renderBuffer = CoreToRender.RenderBridge.Read();

        spriteBatch = new SpriteBatch(GraphicsDevice);

        immediateRenderers = new List<ImmediateRenderer>();

        batchRenderer = new BatchRenderer();
        systemDebugRenderer = new SystemDebugRenderer(main, main.CoreThread);

        immediateRenderers.Add(batchRenderer);
        immediateRenderers.Add(systemDebugRenderer);
    }

    internal void DrawFrame(float partialTick)
    {
        if (CoreToRender.RenderBridge.PendingBuffer)
        {
            renderBuffer = CoreToRender.RenderBridge.Read();
            renderBuffer.Sort();
        }

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        var cameraData = renderBuffer.GetCameraData();

        projectionMatrix = GetProjectionMatrix(viewport, out viewport);
        viewMatrix =       GetViewMatrix(cameraData, viewport, partialTick);

        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;

        batchRenderer.SetData(renderBuffer, partialTick);
        for(int i = 0; i < immediateRenderers.Count; i++)
        {
            immediateRenderers[i].Render();
        }

        Interlocked.Increment(ref frameCount);
        Interlocked.Exchange(ref drawCount, GraphicsDevice.Metrics.DrawCount);
    }

    private Matrix GetViewMatrix(Camera.Data cameraData, Viewport viewport, float partialTick)
    {
        Vector2 position = Vector2.Lerp(cameraData.Position0, cameraData.Position, partialTick);
        float orthoSize  = MathHelper.Lerp(cameraData.OrthographicSize0, cameraData.OrthographicSize, partialTick);

        float scaleY = viewport.Height / orthoSize;
        float scaleX = scaleY;

        var translation = Matrix.CreateTranslation(new Vector3(-position.X, position.Y, 0f));
        var scale = Matrix.CreateScale(scaleX, scaleY, 1f);
        var screenCenter = Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f));

        return translation * scale * screenCenter;
    }

    private Matrix GetProjectionMatrix(Viewport preViewport, out Viewport _viewport)
    {
        var matrix = projectionMatrix;

        Viewport viewport = graphics.GraphicsDevice.Viewport;
        _viewport = preViewport;
        if (viewport.Width != preViewport.Width || viewport.Height != preViewport.Height)
        {
            Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, 4f, -4f, out matrix);
            if (GraphicsDevice.UseHalfPixelOffset)
            {
                matrix.M41 += -0.5f * matrix.M11;
                matrix.M42 += -0.5f * matrix.M22;
            }

            _viewport = viewport;
        }
        return matrix;
    }
}
