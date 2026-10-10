using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Buffers;
using Luminix.Threading.Bridges;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Rendering;

// THE CLASS IS BECOMING A GOD CLASS.

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
        this.instances = new GPUInstance[4096];
        this.commands = new RenderCommand[4096];
    }

    private record struct GPUInstance(Effect Shader, Texture2D Source, Rectangle SourceRect, Color Color, Vector3 Position, Vector2 Scale, float Rotation);
    private record struct RenderCommand(ulong Key, int Index) : IComparable<RenderCommand> { public int CompareTo(RenderCommand other) => Key.CompareTo(other.Key); }

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

    private GPUInstance[] instances;
    private RenderCommand[] commands;
    private int commandCount;

    public bool IsFullScreen => graphics.IsFullScreen;
    public bool IsVSync => graphics.SynchronizeWithVerticalRetrace;

    public GraphicsResourceManager ResourceManager => resourceManager;
    public GraphicsDeviceManager GraphicsDeviceManager => graphics;
    public Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice => graphics.GraphicsDevice;
    public long FrameCount => frameCount;
    public long DrawCount => drawCount;

    public int ResolutionWidth => resolution.Width;
    public int ResolutionHeight => resolution.Height;

    internal void Initialize()
    {
        renderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight);
        unscaledRenderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight);

        renderer = new Renderer();
    }

    internal void NewFrame(float partialTick)
    {
        if (renderBridge.ReadBuffer != renderBuffer)
        {       
            renderBuffer = renderBridge.ReadBuffer;
            
            SetSettings(ref renderBuffer.RenderSettings);
        }

        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;

        if(GraphicsDevice.PresentationParameters.Bounds != screenBounds)
            CreateRenderTargetDestination();

        partialTick = Math.Clamp(partialTick, 0f, 1f);

        var cameraData = renderBuffer?.GetCameraData();

        //World space

        GraphicsDevice.SetRenderTarget(this.renderTarget);
        GraphicsDevice.Clear(Color.Black);
        if(cameraData.HasValue && renderBuffer != null)
        {
            projectionMatrix = GetProjectionMatrix(viewport, out viewport);
            viewMatrix = GetViewMatrix(cameraData, viewport, partialTick);
            renderer.SetTransformMatrix(viewMatrix.Value * projectionMatrix);

            SetData(renderBuffer.GetRenderInstances(), partialTick);

            Logger.NewLog(Logger.Log.Level.Info, $"world {commandCount}");

            for(int i = 0; i < commandCount; i++)
            {
                var command = commands[i];
                var instance = instances[command.Index];
                renderer.Render(instance.Shader, instance.Source, instance.Color, instance.Position, instance.Scale, instance.Rotation, instance.SourceRect);
            }
        }
        
        renderer.Flush();

        //Screen space
        GraphicsDevice.SetRenderTarget(this.unscaledRenderTarget);
        GraphicsDevice.Clear(Color.Transparent);
        projectionMatrix = GetProjectionMatrix(viewport, out viewport);
        renderer.SetTransformMatrix(projectionMatrix);
        
        if(renderBuffer != null)
        {
            SetData(renderBuffer.GetUIRenderInstances());

            for(int i = 0; i < commandCount; i++)
            {
                var instance = instances[i];

                renderer.Render(instance.Shader, instance.Source, instance.Color, instance.Position, instance.Scale, instance.Rotation, instance.SourceRect);
            }


            renderer.Flush();
        }

        //final
        GraphicsDevice.SetRenderTarget(null);
        renderer.Render(resourceManager.GenericShader, renderTarget, Color.White, in renderTargetDestination);
        renderer.Render(resourceManager.GenericShader, unscaledRenderTarget, Color.White, in unscaledRenderTargetDestination);
        renderer.Flush();

        frameCount++;
        drawCount = GraphicsDevice.Metrics.DrawCount;
    }

    private void SetSettings(ref RenderSettings renderSettings)
    {
        var settingsChanged = false;

        var resolution = renderSettings.Resolution;
        var frameRate = renderSettings.FrameRate;
        var vSync = renderSettings.VSync;

        //TODO frameRate

        if(this.resolution.X != resolution.X || this.resolution.Y != resolution.Y)
        {
            this.resolution = new Rectangle(0, 0, resolution.X, resolution.Y);
            settingsChanged = true;
        }

        if(this.IsVSync != vSync)
        {
            this.graphics.SynchronizeWithVerticalRetrace = vSync;
            settingsChanged = true;
        }

        if(settingsChanged)
            Apply();
    }

    public void SetWindowMode(Window.Mode windowMode)
    {
        this.graphics.IsFullScreen = windowMode == Window.Mode.Fullscreen;
    }
    public void Apply()
    {
        var prevWindowPos = new Vector2(window.ClientBounds.X, window.ClientBounds.Y);
        MouseState mouse = Mouse.GetState();

        graphics.ApplyChanges();

        if(resolution.Width != renderTarget.Width || resolution.Height != renderTarget.Height)
        {
            renderTarget.Dispose();
            renderTarget = new RenderTarget2D(GraphicsDevice, resolution.Width, resolution.Height);
        }

        var currWindowPos = new Vector2(window.ClientBounds.X, window.ClientBounds.Y);
        var pos = prevWindowPos - currWindowPos;
        Mouse.SetPosition((int)(pos.X + mouse.X), (int)(pos.Y + mouse.Y));
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

            int posX = screenSize.Width - newWidth / 2; // we do it like and not "(screenSize.Width - newWidth) / 2" because we have the quad vertices from -0.5 to 0.5
            int posY = screenSize.Height - newHeight / 2;

            renderTargetDestination = new Rectangle(posX, posY, newWidth, newHeight);
        }

        {
            float scaleX = (float)screenSize.Width / unscaledRenderTarget.Width;
            float scaleY = (float)screenSize.Height / unscaledRenderTarget.Height;
            float scale = Math.Min(scaleX, scaleY);

            int newWidth = (int)(unscaledRenderTarget.Width * scale);
            int newHeight = (int)(unscaledRenderTarget.Height * scale);

            int posX = screenSize.Width - newWidth / 2;
            int posY = screenSize.Height - newHeight / 2;

            unscaledRenderTargetDestination = new Rectangle(posX, posY, newWidth, newHeight);
        }

        screenBounds = screenSize;
    }

    private void SetData(ReadOnlySpan<RenderInstance> newInstances, float partialTick)
    {
        commandCount = newInstances.Length;
        if (commandCount == 0) return;

        EnsureCapacity(commands, instances, commandCount);

        for (int i = 0; i < commandCount; i++)
        {
            try
            {
                ref readonly var instance = ref newInstances[i];

                ShaderResource shaderResource = instance.Shader;
                TextureResource textureResource = instance.Texture;

                var transform = Interpolate(instance.Transform, partialTick);

                ushort layerBits = (ushort)Math.Clamp((int)(transform.Position.X * 100.0f), 0, 65535);
                var key = BuildKey(layerBits, shaderResource, textureResource);

                commands[i] = new RenderCommand(key, i);

                instances[i] = new GPUInstance(resourceManager.GetShader(shaderResource), resourceManager.GetTexture(textureResource), textureResource.Rectangle, instance.Color, transform.Position, transform.Scale, transform.Rotation);
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to render instance, reason: {ex}");
                i--;
                commandCount--;
            }
        }

        Array.Sort(commands, 0, commandCount);
    }
    private void SetData(ReadOnlySpan<RenderInstanceUI> newInstances)
    {
        commandCount = newInstances.Length;
        if (commandCount == 0) return;

        EnsureCapacity(commands, instances, commandCount);

        for (int i = 0; i < commandCount; i++)
        {
            try
            {
                ref readonly var instance = ref newInstances[i];

                Vector3 position = new Vector3(instance.Position.X, instance.Position.Y, 0);
                Vector2 scale = new Vector2(instance.Scale.X, instance.Scale.Y);

                ShaderResource shaderResource = instance.Shader;
                TextureResource textureResource = instance.Texture;

                instances[i] = new GPUInstance(resourceManager.GetShader(shaderResource), resourceManager.GetTexture(textureResource), textureResource.Rectangle, instance.Color, position, scale, instance.Rotation);
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to render instance, reason: {ex}");
                i--;
                commandCount--;
            }
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void EnsureCapacity(RenderCommand[] commands, GPUInstance[] instances, int requiredCapacity)
    {
        if (commands.Length < requiredCapacity)
        {
            int newCapacity = commands.Length * 2;
            while (newCapacity < requiredCapacity)
                newCapacity *= 2;

            Array.Resize(ref commands, newCapacity);
            Array.Resize(ref instances, newCapacity);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static Transform Interpolate(in TransformInterp transform, float partialTick)
    {
        return new Transform(
            Vector3.Lerp(transform.Position0, transform.Position, partialTick),
            Vector2.Lerp(transform.Scale0, transform.Scale, partialTick),
            MathHelper.Lerp(transform.Rotation0, transform.Rotation, partialTick)
        );
    }

    // public void TakeScreenshot(float partialTick)
    // {
    //     string screenshotPath = Path.Combine("Screenshots", $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

    //     try
    //     {
    //         if (!Directory.Exists("Screenshots"))
    //             Directory.CreateDirectory("Screenshots");

    //         using (var stream = new FileStream(screenshotPath, FileMode.Create))
    //         {
    //             // var screenShotRect = new Rectangle(0, 0, Settings.ScreenshotWidth, Settings.ScreenshotHeight);
    //             // var screenShotTarget = new RenderTarget2D(GraphicsDevice, Settings.ScreenshotWidth, Settings.ScreenshotHeight);

    //             // TODO : add screenshot resolution settings

    //             var screenShotRect = new Rectangle(0, 0, 1920, 1080);
    //             var screenShotTarget = new RenderTarget2D(GraphicsDevice, 1920, 1080);

    //             NewFrame(partialTick);

    //             var renderTargetDestination = this.renderTargetDestination;
    //             var unscaledRenderTargetDestination = this.unscaledRenderTargetDestination;

    //             {
    //                 float scaleX = (float)screenShotTarget.Width / renderTarget.Width;
    //                 float scaleY = (float)screenShotTarget.Height / renderTarget.Height;
    //                 float scale = Math.Min(scaleX, scaleY);

    //                 int newWidth = (int)(renderTarget.Width * scale);
    //                 int newHeight = (int)(renderTarget.Height * scale);

    //                 int posX = (screenShotTarget.Width - newWidth) / 2;
    //                 int posY = (screenShotTarget.Height - newHeight) / 2;

    //                 renderTargetDestination = new Rectangle(posX, posY, newWidth, newHeight);
    //             }
    //             {
    //                 float scaleX = (float)screenShotTarget.Width / unscaledRenderTarget.Width;
    //                 float scaleY = (float)screenShotTarget.Height / unscaledRenderTarget.Height;
    //                 float scale = Math.Min(scaleX, scaleY);

    //                 int newWidth = (int)(unscaledRenderTarget.Width * scale);
    //                 int newHeight = (int)(unscaledRenderTarget.Height * scale);

    //                 int posX = (screenShotTarget.Width - newWidth) / 2;
    //                 int posY = (screenShotTarget.Height - newHeight) / 2;

    //                 unscaledRenderTargetDestination = new Rectangle(posX, posY, newWidth, newHeight);
    //             }

    //             GraphicsDevice.SetRenderTarget(screenShotTarget);

    //             spriteBatch.Begin();
    //             spriteBatch.Draw(this.renderTarget, renderTargetDestination, Color.White);
    //             spriteBatch.Draw(this.unscaledRenderTarget, unscaledRenderTargetDestination, Color.White);
    //             spriteBatch.End();

    //             GraphicsDevice.SetRenderTarget(null);

    //             screenShotTarget.SaveAsPng(stream, screenShotRect.Width, screenShotRect.Height);
    //             screenShotTarget.Dispose();

    //             Logger.NewLog(Logger.Log.Level.Info, $"Screenshot saved to: {screenshotPath}");
    //         }

    //         GC.Collect();
    //     }
    //     catch (Exception e)
    //     {
    //         Logger.NewLog(Logger.Log.Level.Error, $"RenderingSystem: failed to produce a screenshot, reason: {e.Message}");
    //     }
    // }
}
