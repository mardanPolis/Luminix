using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Resources;
using Luminix.Simulation;
using System.Collections.Generic;
using Luminix.Identifiers;
using System.IO;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Rendering;

public sealed class RenderingSystem : Workspace.Service
{
    internal RenderingSystem(Main main)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(main.GraphicsDeviceManager);

        this.main = main;
        this.graphics = main.GraphicsDeviceManager;
        this.window = main.Window;
        this.resourceManager = main.ResourceManager;

        resolution = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
    }

    private Main main;
    private BatchRenderer batchRenderer;
    private SystemDebugRenderer systemDebugRenderer;
    private GraphicsDeviceManager graphics;
    private GameWindow window;
    private ResourceManager resourceManager;

    private FontResource defaultFontBold;

    private RenderBuffer renderBuffer;

    private SpriteBatch spriteBatch;
    private RenderTarget2D renderTarget;
    private RenderTarget2D unscaledRenderTarget;
    private Rectangle renderTargetDestination;
    private Rectangle unscaledRenderTargetDestination;

    private Rectangle resolution;
    private Rectangle screenBounds;

    private ulong frameCount = 0;
    private long drawCount = 0;

    private Viewport viewport;

    private Matrix? viewMatrix;
    private Matrix projectionMatrix;

    private bool changedResolution = false;
    private bool changedFullscreen = false;

    public bool IsFullScreen => graphics.IsFullScreen;

    public ResourceManager ResourceManager => resourceManager;
    public GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public ulong FrameCount => Interlocked.Read(ref frameCount);
    public long DrawCount => Interlocked.Read(ref drawCount);
    public Viewport Viewport => viewport;

    public int ResolutionWidth => resolution.Width;
    public int ResolutionHeight => resolution.Height;

    public Matrix? ViewMatrix => viewMatrix;
    public Matrix ProjectionMatrix => projectionMatrix;
    public Matrix? TransformMatrix => viewMatrix.HasValue ? viewMatrix.Value * projectionMatrix : null;

    public double FrameTime => main.FrameTime;
    public double FrameDeltaTime => main.FrameDeltaTime;
    public double FPS => main.FPS;
    public long FrameRate => main.FrameRate;
    public long FrameAllocationAmount => main.FrameAllocationAmount;


    internal SpriteBatch SpriteBatch => spriteBatch;

    //TODO : add a way to create ImmediateRenderers

    List<ImmediateRenderer> immediateRenderers;

    internal void Initialize()
    {
        renderBuffer = CoreToRender.RenderBridge.Read();

        spriteBatch = new SpriteBatch(GraphicsDevice);
        renderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight);
        unscaledRenderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight);

        immediateRenderers = new List<ImmediateRenderer>();

        batchRenderer = new BatchRenderer();
        immediateRenderers.Add(batchRenderer);

        defaultFontBold = resourceManager.ResourceProvider.GetResource<FontResource>("IBMPlexMono-Bold");

        if(Settings.Debug)
        {
            systemDebugRenderer = new SystemDebugRenderer(main, main.CoreThread);
            immediateRenderers.Add(systemDebugRenderer);
        }
    }

    internal void NewFrame(float partialTick, RenderTarget2D renderTarget = null)
    {
        if (CoreToRender.RenderBridge.PendingBuffer)
        {
            renderBuffer = CoreToRender.RenderBridge.Read();
            renderBuffer.Sort();
        }

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        var cameraData = renderBuffer.GetCameraData();

        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;

        if(GraphicsDevice.PresentationParameters.Bounds != screenBounds)
            CreateRenderTargetDestination();

        GraphicsDevice.SetRenderTarget(this.renderTarget);
        GraphicsDevice.Clear(Color.Black);
        projectionMatrix = GetProjectionMatrix(viewport, out viewport);
        viewMatrix = GetViewMatrix(cameraData, viewport, partialTick);
        
        batchRenderer.SetData(renderBuffer, partialTick);

        for(int i = 0; i < immediateRenderers.Count; i++)
        {
            immediateRenderers[i].Render();
        }

        GraphicsDevice.SetRenderTarget(this.unscaledRenderTarget);
        GraphicsDevice.Clear(Color.Transparent);
        projectionMatrix = GetProjectionMatrix(viewport, out viewport);
        viewMatrix = GetViewMatrix(cameraData, viewport, partialTick);
    
        for(int i = 0; i < immediateRenderers.Count; i++)
        {
            immediateRenderers[i].UnscaledRender();
        }

        GraphicsDevice.SetRenderTarget(renderTarget);
        spriteBatch.Begin();
        spriteBatch.Draw(this.renderTarget, renderTargetDestination, Color.White);
        spriteBatch.Draw(this.unscaledRenderTarget, unscaledRenderTargetDestination, Color.White);
        spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        
        Interlocked.Increment(ref frameCount);
        Interlocked.Exchange(ref drawCount, GraphicsDevice.Metrics.DrawCount);
    }

    public void TakeScreenshot(float partialTick)
    {
        string screenshotPath = Path.Combine("Screenshots", $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        try
        {
            if (!Directory.Exists("Screenshots"))
                Directory.CreateDirectory("Screenshots");

            using (var stream = new FileStream(screenshotPath, FileMode.Create))
            {
                var screenShotRect = new Rectangle(0, 0, Settings.ScreenshotWidth, Settings.ScreenshotHeight);
                var screenShotTarget = new RenderTarget2D(GraphicsDevice, Settings.ScreenshotWidth, Settings.ScreenshotHeight);

                NewFrame(partialTick, screenShotTarget);
                screenShotTarget.SaveAsPng(stream, screenShotRect.Width, screenShotRect.Height);
                screenShotTarget.Dispose();

                Logger.Get().Log($"Screenshot saved to: {screenshotPath}");
            }

            GC.Collect();
        }
        catch (Exception e)
        {
            Logger.Get().Err($"Failed to save screenshot. Error: {e.Message}");
        }
    }

    public void SetResolution(int width, int height)
    {
        resolution = new Rectangle(0, 0, width, height);
        changedResolution = true;
    }

    public void Fullscreen(bool enable)
    {
        graphics.IsFullScreen = enable;
        changedFullscreen = true;
    }

    public void Apply()
    {
        var prevWindowPos = new Vector2(window.ClientBounds.X, window.ClientBounds.Y);
        MouseState mouse = Mouse.GetState();

        graphics.ApplyChanges();

        if(changedResolution && (resolution.Width != renderTarget.Width || resolution.Height != renderTarget.Height))
        {
            renderTarget.Dispose();
            renderTarget = new RenderTarget2D(GraphicsDevice, resolution.Width, resolution.Height);
        }

        if(changedFullscreen)
        {
            var currWindowPos = new Vector2(window.ClientBounds.X, window.ClientBounds.Y);

            var pos = prevWindowPos - currWindowPos;

            Mouse.SetPosition((int)(pos.X + mouse.X), (int)(pos.Y + mouse.Y));
        }

        changedResolution = false;
        changedFullscreen = false;
    }

    private Matrix? GetViewMatrix(Camera.Data? cameraData, Viewport viewport, float partialTick)
    {
        if(cameraData == null) return null;

        Camera.Data cameraDataValue = cameraData.Value;
        Vector2 position = Vector2.Lerp(cameraDataValue.Position0, cameraDataValue.Position, partialTick);
        float orthoSize  = MathHelper.Lerp(cameraDataValue.OrthographicSize0, cameraDataValue.OrthographicSize, partialTick);

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

    private void CreateRenderTargetDestination()
    {
        var screenSize = GraphicsDevice.PresentationParameters.Bounds;

        {
            float scaleX = (float)screenSize.Width / renderTarget.Width;
            float scaleY = (float)screenSize.Height / renderTarget.Height;
            float scale = Math.Min(scaleX, scaleY);

            int newWidth = (int)(renderTarget.Width * scale);
            int newHeight = (int)(renderTarget.Height * scale);

            int posX = (screenSize.Width - newWidth) / 2;
            int posY = (screenSize.Height - newHeight) / 2;

            renderTargetDestination = new Rectangle(posX, posY, newWidth, newHeight);
        }

        {
            float scaleX = (float)screenSize.Width / unscaledRenderTarget.Width;
            float scaleY = (float)screenSize.Height / unscaledRenderTarget.Height;
            float scale = Math.Min(scaleX, scaleY);

            int newWidth = (int)(unscaledRenderTarget.Width * scale);
            int newHeight = (int)(unscaledRenderTarget.Height * scale);

            int posX = (screenSize.Width - newWidth) / 2;
            int posY = (screenSize.Height - newHeight) / 2;

            unscaledRenderTargetDestination = new Rectangle(posX, posY, newWidth, newHeight);
        }

        screenBounds = screenSize;
    }
}
