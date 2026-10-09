using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Buffers;
using Luminix.Threading.Bridges;
using System.Runtime.CompilerServices;

namespace Luminix.Rendering;

public sealed class RenderingSystem : Workspace.Service
{
    internal RenderingSystem(MainRender main, TripleBufferBridge<RenderBuffer> renderBridge)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(main.GraphicsDeviceManager);

        this.main = main;
        this.graphics = main.GraphicsDeviceManager;
        this.window = main.Window;
        this.resourceManager = main.ResourceManager;

        resolution = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

        this.renderBridge = renderBridge;
    }

    private MainRender main;
    private Renderer renderer;
    private GraphicsDeviceManager graphics;
    private GameWindow window;
    private GraphicsResourceManager resourceManager;

    private TripleBufferBridge<RenderBuffer> renderBridge;
    private RenderBuffer renderBuffer;

    private RenderTarget2D renderTarget;
    private RenderTarget2D unscaledRenderTarget;
    private Rectangle renderTargetDestination;
    private Rectangle unscaledRenderTargetDestination;

    private Rectangle resolution;
    private Rectangle screenBounds;

    private long frameCount = 0;
    private long drawCount = 0;

    private Viewport viewport;

    private Matrix? viewMatrix;
    private Matrix projectionMatrix;

    public bool IsFullScreen => graphics.IsFullScreen;
    public bool IsVSync => graphics.SynchronizeWithVerticalRetrace;

    public GraphicsResourceManager ResourceManager => resourceManager;
    public GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public long FrameCount => frameCount;
    public long DrawCount => drawCount;
    public Viewport Viewport => viewport;

    public int ResolutionWidth => resolution.Width;
    public int ResolutionHeight => resolution.Height;

    public Matrix? ViewMatrix => viewMatrix;
    public Matrix ProjectionMatrix => projectionMatrix;
    public Matrix? TransformMatrix => viewMatrix.HasValue ? viewMatrix.Value * projectionMatrix : null;

    internal SpriteBatch SpriteBatch => spriteBatch;

    internal void Initialize()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        renderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight);
        unscaledRenderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight);

        renderer = new Renderer();
    }

    internal void NewFrame(float partialTick)
    {
        if (renderBridge.ReadBuffer != renderBuffer)
        {       
            renderBuffer = renderBridge.ReadBuffer;
            
            ref RenderSettings renderSettings = ref renderBuffer.RenderSettings;

            var resolution = renderSettings.Resolution;
            var frameRate = renderSettings.FrameRate;
            var vSync = renderSettings.VSync;

            //TODO set settings
        }


        var cameraData = renderBuffer?.GetCameraData();

        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;

        if(GraphicsDevice.PresentationParameters.Bounds != screenBounds)
            CreateRenderTargetDestination();

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        //scaled
        GraphicsDevice.SetRenderTarget(this.renderTarget);
        GraphicsDevice.Clear(Color.Black);
        projectionMatrix = GetProjectionMatrix(viewport, out viewport);
        viewMatrix = GetViewMatrix(cameraData, viewport, partialTick);
        
        renderer.SetData(renderBuffer.GetRenderInstances(), partialTick);
        renderer.Render();

        //unscaled
        GraphicsDevice.SetRenderTarget(this.unscaledRenderTarget);
        GraphicsDevice.Clear(Color.Transparent);
        projectionMatrix = GetProjectionMatrix(viewport, out viewport);
        viewMatrix = GetViewMatrix(cameraData, viewport, partialTick);
    
        renderer.SetData(renderBuffer.GetUIRenderInstances());
        renderer.Render();

        //final
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        renderer.Render(this.renderTarget, renderTargetDestination, Color.White);
        spriteBatch.Draw(this.unscaledRenderTarget, unscaledRenderTargetDestination, Color.White);
        spriteBatch.End();

        frameCount++;
        drawCount = GraphicsDevice.Metrics.DrawCount;
    }

    private Matrix? GetViewMatrix(in Camera? cameraData, Viewport viewport, float partialTick)
    {
        if(cameraData == null) return null;

        Camera camera = cameraData.Value;
        Vector2 position = Vector2.Lerp(Camera.GetPosition(in camera), Camera.GetTargetPosition(in camera), partialTick);
        float orthoSize  = MathHelper.Lerp(Camera.GetOrthographicSize(in camera), Camera.GetTargetOrthographicSize(in camera), partialTick);

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


    public void SetData(ReadOnlySpan<RenderInstance> instances, float partialTick)
    {
        activeCommandCount = instances.Length;
        if (activeCommandCount == 0) return;

        EnsureCapacity(activeCommandCount);

        for (int i = 0; i < instances.Length; i++)
        {
            ref readonly var instance = ref instances[i];

            var transform = Interpolate(instance.Transform, partialTick);

            ushort layerBits = (ushort)Math.Clamp((int)(transform.Position.X * 100.0f), 0, 65535);

            var key = BuildKey(layerBits, instance.Shader, instance.Resource);

            commands[i] = new RenderCommand(key, i);
            gpuInstanceBuffer[i] = new GPUInstanceData(instance.Shader, instance.Resource, instance.Color, transform, instance.Text);
        }

        Array.Sort(commands, 0, activeCommandCount);
    }
    public void SetData(ReadOnlySpan<RenderInstanceUI> instances)
    {
        activeCommandCount = instances.Length;
        if (activeCommandCount == 0) return;

        EnsureCapacity(activeCommandCount);

        for (int i = 0; i < instances.Length; i++)
        {
            ref readonly var instance = ref instances[i];

            var transform = new Transform(new Vector3(instance.X, instance.Y, 0), instance.Rotation);

            ushort layerBits = (ushort)Math.Clamp((int)(transform.Position.X * 100.0f), 0, 65535);

            var key = BuildKey(layerBits, instance.Shader, instance.Resource);

            commands[i] = new RenderCommand(key, i);
            gpuInstanceBuffer[i] = new GPUInstanceData(instance.Shader, instance.Resource, instance.Color, transform, instance.Text);
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] private void EnsureCapacity(int requiredCapacity)
    {
        if (commands.Length < requiredCapacity)
        {
            int newCapacity = Math.Max(commands.Length * 2, requiredCapacity);
            Array.Resize(ref commands, newCapacity);
            Array.Resize(ref gpuInstanceBuffer, newCapacity);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static ulong BuildKey(uint layer, ShaderResource shader, Resource resource)
    {
        uint resourceIndex = resource is TextureResource texture ? texture.Index : ((FontResource)resource).Index;

        ulong layerBits = ((ulong)layer & 0xFFFF) << 48;
        ulong shaderBits = ((ulong)(shader.Index & 0xFF_FFFF)) << 24;
        ulong textureBits = (ulong)(resourceIndex & 0xFF_FFFF);

        return layerBits | shaderBits | textureBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    private static Transform Interpolate(in TransformInterp transform, float partialTick)
    {
        return new Transform(
            Vector3.Lerp(transform.Position0, transform.Position, partialTick),
            Vector2.Lerp(transform.Scale0, transform.Scale, partialTick),
            MathHelper.Lerp(transform.Rotation0, transform.Rotation, partialTick)
        );
    }
}
