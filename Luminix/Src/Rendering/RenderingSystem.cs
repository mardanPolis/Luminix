using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading.Buffers;
using Luminix.Threading.Bridges;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Rendering;

// THE CLASS IS BECOMING A GOD CLASS.

internal sealed class RenderingSystem : Workspace.Service
{
    internal RenderingSystem(MainRender main, TripleBufferBridge<RenderBuffer> renderBridge)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(main.GraphicsDeviceManager);
 
        this.main = main;
        this.graphicManager = main.GraphicsDeviceManager;
        this.window = main.Window;
        this.resourceManager = main.ResourceManager;
 
        resolution = new Rectangle(0, 0, graphicManager.PreferredBackBufferWidth, graphicManager.PreferredBackBufferHeight);
 
        this.renderBridge = renderBridge;
        this.instances = new GPUInstance[4096];
        this.commands = new RenderCommand[4096];
    }
 
    private record struct GPUInstance(Effect Shader, Texture2D Source, Rectangle SourceRect, Color Color, Vector3 Position, Vector2 Scale, float Rotation);
    private record struct RenderCommand(ulong Key, int Index) : IComparable<RenderCommand> { public int CompareTo(RenderCommand other) => Key.CompareTo(other.Key); }
 
    private MainRender main;
    private Renderer renderer;
    private GraphicsDeviceManager graphicManager;
    private GameWindow window;
    private GraphicsResourceManager resourceManager;
 
    private TripleBufferBridge<RenderBuffer> renderBridge;
    private RenderBuffer renderBuffer;
 
    // World target: fixed internal resolution (the "virtual" resolution), letterboxed onto the back buffer.
    private RenderTarget2D renderTarget;
    // UI target: always the size of the back buffer, drawn 1:1 over the whole window.
    private RenderTarget2D unscaledRenderTarget;
    private Rectangle renderTargetDestination;
    private Rectangle unscaledRenderTargetDestination;
 
    // Internal (virtual) resolution.
    private Rectangle resolution;
 
    private float targetFrameRate;
 
    private long frameCount = 0;
    private long drawCount = 0;
 
    private bool integerScaling = false;
 
    private GPUInstance[] instances;
    private RenderCommand[] commands;
    private int commandCount;
 
    public bool IsFullScreen => graphicManager.IsFullScreen;
    public bool IsVSync => graphicManager.SynchronizeWithVerticalRetrace;
 
    public GraphicsResourceManager ResourceManager => resourceManager;
    public GraphicsDeviceManager GraphicsDeviceManager => graphicManager;
    public Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice => graphicManager.GraphicsDevice;
    public long FrameCount => frameCount;
    public long DrawCount => drawCount;
 
    public int ResolutionWidth => resolution.Width;
    public int ResolutionHeight => resolution.Height;
 
    public int BackBufferWidth => GraphicsDevice.PresentationParameters.BackBufferWidth;
    public int BackBufferHeight => GraphicsDevice.PresentationParameters.BackBufferHeight;
 
    public Rectangle WorldDestination => renderTargetDestination;
 
    public bool IntegerScaling
    {
        get => integerScaling;
        set
        {
            if (integerScaling == value) return;
            integerScaling = value;
            ValidateRenderTargetDestination();
        }
    }
 
    protected override void Initialize()
    {
        var pp = GraphicsDevice.PresentationParameters;
 
        renderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight);
        unscaledRenderTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight);
 
        renderer = new Renderer();
 
        ValidateRenderTargetDestination();
    }
 
    internal void NewFrame(float partialTick)
    {
        if (renderBridge.ReadBuffer != renderBuffer)
        {
            resourceManager.HandleCommands();
 
            renderBuffer = renderBridge.ReadBuffer;
 
            SetSettings(ref renderBuffer.RenderSettings);
 
            ValidateRenderTargetDestination();
        }
 
        // The window may have been resized by the user / OS (or fullscreen toggled).
        SyncBackBuffer();
 
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
 
        partialTick = Math.Clamp(partialTick, 0f, 1f);
 
        if (renderBuffer != null)
        {
            var cameraData = renderBuffer.GetCameraData();
 
            GraphicsDevice.SetRenderTarget(renderTarget);
 
            if (cameraData != null)
            {
                var projection = CreateProjectionMatrix(renderTarget.Width, renderTarget.Height);
                var view = GetViewMatrix(cameraData, renderTarget.Width, renderTarget.Height, partialTick);
 
                renderer.SetTransformMatrix(view.Value * projection);
                renderer.Reset();
 
                GraphicsDevice.Clear(cameraData.Value.BackgroundColor);
 
                SetData(renderBuffer.GetRenderInstances(), partialTick);
 
                for (int i = 0; i < commandCount; i++)
                {
                    var command = commands[i];
                    var instance = instances[command.Index];
                    renderer.Render(instance.Shader, instance.Source, instance.Color, instance.Position, instance.Scale, instance.Rotation, instance.SourceRect);
                }
 
                renderer.Flush();
                renderer.Reset();
            }
            else
            {
                GraphicsDevice.Clear(Color.Black);
            }
 
            GraphicsDevice.SetRenderTarget(unscaledRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
 
            renderer.SetTransformMatrix(CreateProjectionMatrix(unscaledRenderTarget.Width, unscaledRenderTarget.Height));
            renderer.Reset();
            SetData(renderBuffer.GetUIRenderInstances());
 
            for (int i = 0; i < commandCount; i++)
            {
                var instance = instances[i];
                renderer.Render(instance.Shader, instance.Source, instance.Color, instance.Position, instance.Scale, instance.Rotation, instance.SourceRect);
            }
 
            renderer.Flush();
            renderer.Reset();
 
            var shader = resourceManager.GetShader(new ShaderID(0));
 
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black); // letterbox / pillarbox bars
 
            renderer.SetTransformMatrix(CreateProjectionMatrix(BackBufferWidth, BackBufferHeight));
            renderer.Reset();
            renderer.Render(shader, renderTarget, Color.White, in renderTargetDestination);
            renderer.Render(shader, unscaledRenderTarget, Color.White, in unscaledRenderTargetDestination);
            renderer.Flush();
 
            frameCount++;
            drawCount = GraphicsDevice.Metrics.DrawCount;
        }
        else
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
        }
    }
 
    private void SetSettings(ref RenderSettings renderSettings)
    {
        var settingsChanged = false;
 
        var resolution = renderSettings.Resolution;
        var frameRate = renderSettings.FrameRate;
        var vSync = renderSettings.VSync;
 
        if (frameRate != targetFrameRate)
        {
            targetFrameRate = frameRate;
            main.TargetElapsedTime = TimeSpan.FromSeconds(1d / frameRate);
        }
 
        if (this.resolution.Width != resolution.X || this.resolution.Height != resolution.Y)
        {
            SetWindowSize(resolution.X, resolution.Y);
 
            this.resolution = new Rectangle(0, 0, resolution.X, resolution.Y);
            settingsChanged = true;
        }
 
        if (this.IsVSync != vSync)
        {
            this.graphicManager.SynchronizeWithVerticalRetrace = vSync;
            settingsChanged = true;
        }
 
        if (settingsChanged)
            Apply();
    }
 
    public void SetWindowSize(int width, int height)
    {
        graphicManager.PreferredBackBufferWidth = width;
        graphicManager.PreferredBackBufferHeight = height;
    }
 
    public void SetWindowMode(Window.Mode windowMode)
    {
        this.graphicManager.IsFullScreen = windowMode == Window.Mode.Fullscreen;
        Apply();
    }
 
    public void Apply()
    {
        var prevWindowPos = new Vector2(window.ClientBounds.X, window.ClientBounds.Y);
        MouseState mouse = Mouse.GetState();
 
        graphicManager.ApplyChanges();
 
        // Internal resolution changed -> recreate the world target.
        if (resolution.Width != renderTarget.Width || resolution.Height != renderTarget.Height)
        {
            renderTarget.Dispose();
            renderTarget = new RenderTarget2D(GraphicsDevice, resolution.Width, resolution.Height);
        }
 
        // Back buffer changed -> recreate the UI target.
        SyncBackBuffer();
 
        ValidateRenderTargetDestination();
 
        var currWindowPos = new Vector2(window.ClientBounds.X, window.ClientBounds.Y);
        var pos = prevWindowPos - currWindowPos;
        Mouse.SetPosition((int)(pos.X + mouse.X), (int)(pos.Y + mouse.Y));
    }
 
    /// <summary>
    /// Keeps the UI target the same size as the back buffer and refreshes the destination rectangles.
    /// </summary>
    private void SyncBackBuffer()
    {
        int w = BackBufferWidth;
        int h = BackBufferHeight;
 
        if (unscaledRenderTarget.Width == w && unscaledRenderTarget.Height == h)
            return;
 
        unscaledRenderTarget.Dispose();
        unscaledRenderTarget = new RenderTarget2D(GraphicsDevice, w, h);
 
        ValidateRenderTargetDestination();
    }
 
    /// <summary>
    /// Maps a window (back buffer) pixel position to a position inside the internal resolution.
    /// Returns false when the point lies in the letterbox bars.
    /// </summary>
    public bool TryWindowToResolution(Point windowPoint, out Vector2 resolutionPoint)
    {
        var dest = renderTargetDestination;
        resolutionPoint = default;
 
        if (dest.Width <= 0 || dest.Height <= 0 || !dest.Contains(windowPoint))
            return false;
 
        resolutionPoint = new Vector2(
            (windowPoint.X - dest.X) * (resolution.Width / (float)dest.Width),
            (windowPoint.Y - dest.Y) * (resolution.Height / (float)dest.Height));
        return true;
    }
 
    private Matrix? GetViewMatrix(in Camera? cameraData, int targetWidth, int targetHeight, float partialTick)
    {
        if (cameraData == null) return null;
 
        Camera camera = cameraData.Value;
        Vector2 position = Vector2.Lerp(Camera.GetPosition(in camera), Camera.GetTargetPosition(in camera), partialTick);
        float orthoSize = MathHelper.Lerp(Camera.GetOrthographicSize(in camera), Camera.GetTargetOrthographicSize(in camera), partialTick);
 
        // orthoSize = number of world units visible vertically at the internal resolution.
        float scaleY = targetHeight / orthoSize;
        float scaleX = scaleY;
 
        var translation = Matrix.CreateTranslation(new Vector3(-position.X, position.Y, 0f));
        var scale = Matrix.CreateScale(scaleX, scaleY, 1f);
        var screenCenter = Matrix.CreateTranslation(new Vector3(targetWidth * 0.5f, targetHeight * 0.5f, 0f));
 
        return translation * scale * screenCenter;
    }
 
    private Matrix CreateProjectionMatrix(int width, int height)
    {
        Matrix.CreateOrthographicOffCenter(0f, width, height, 0f, 400f, -400f, out Matrix matrix);
 
        if (GraphicsDevice.UseHalfPixelOffset)
        {
            matrix.M41 += -0.5f * matrix.M11;
            matrix.M42 += -0.5f * matrix.M22;
        }
 
        return matrix;
    }
 
    private void ValidateRenderTargetDestination()
    {
        if (renderTarget == null || unscaledRenderTarget == null)
            return;
 
        int backWidth = BackBufferWidth;
        int backHeight = BackBufferHeight;
 
        renderTargetDestination = FitRectangle(renderTarget.Width, renderTarget.Height, backWidth, backHeight, integerScaling);
 
        unscaledRenderTargetDestination = new Rectangle(0, 0, backWidth, backHeight);
    }
 
    private static Rectangle FitRectangle(int sourceWidth, int sourceHeight, int destWidth, int destHeight, bool integerScale)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0 || destWidth <= 0 || destHeight <= 0)
            return new Rectangle(0, 0, Math.Max(destWidth, 0), Math.Max(destHeight, 0));
 
        float scale = Math.Min((float)destWidth / sourceWidth, (float)destHeight / sourceHeight);
 
        if (integerScale && scale >= 1f)
            scale = MathF.Floor(scale);
 
        int width = Math.Max(1, (int)MathF.Round(sourceWidth * scale));
        int height = Math.Max(1, (int)MathF.Round(sourceHeight * scale));
 
        return new Rectangle((destWidth - width) / 2, (destHeight - height) / 2, width, height);
    }
 
    private void SetData(ReadOnlySpan<RenderInstance> newInstances, float partialTick)
    {
        commandCount = newInstances.Length;
        if (commandCount == 0) return;
 
        EnsureCapacity(commandCount);
 
        for (int i = 0; i < commandCount; i++)
        {
            try
            {
                ref readonly var instance = ref newInstances[i];
 
                ShaderID shader = instance.Shader;
                TextureID texture = instance.Texture;
 
                Transform transform = Interpolate(instance.Transform, partialTick);
 
                var key = BuildKey(transform.Position.Z, shader, texture);
 
                var transformPos = transform.Position;
                var position = new Vector3(transformPos.X, -transformPos.Y, transformPos.Z);
 
                commands[i] = new RenderCommand(key, i);
 
                instances[i] = new GPUInstance(resourceManager.GetShader(shader), resourceManager.GetTexture(texture), instance.SourceRectangle, instance.Color, position, transform.Scale, transform.Rotation);
            }
            catch (Exception ex)
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
 
        EnsureCapacity(commandCount);
 
        for (int i = 0; i < commandCount; i++)
        {
            try
            {
                ref readonly var instance = ref newInstances[i];
 
                Vector3 position = new Vector3(instance.Position.X, instance.Position.Y, 0);
                Vector2 scale = new Vector2(instance.Scale.X, instance.Scale.Y);
 
                ShaderID shader = instance.Shader;
                TextureID texture = instance.Texture;
 
                instances[i] = new GPUInstance(resourceManager.GetShader(shader), resourceManager.GetTexture(texture), instance.SourceRectangle, instance.Color, position, scale, instance.Rotation);
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to render instance, reason: {ex}");
                i--;
                commandCount--;
            }
        }
    }
 
    // Instance method on purpose: Array.Resize needs to write back to the fields.
    // (The old static version resized local copies of the references, so the real arrays never grew.)
    private void EnsureCapacity(int requiredCapacity)
    {
        if (commands.Length >= requiredCapacity && instances.Length >= requiredCapacity)
            return;
 
        int newCapacity = Math.Max(commands.Length, 1) * 2;
        while (newCapacity < requiredCapacity)
            newCapacity *= 2;
 
        Array.Resize(ref commands, newCapacity);
        Array.Resize(ref instances, newCapacity);
    }
 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong BuildKey(float layer, ShaderID shader, TextureID texture)
    {
        uint bits = BitConverter.SingleToUInt32Bits(layer);
        bits = (bits & 0x8000_0000) != 0 ? ~bits : bits | 0x8000_0000;

        return ((ulong)bits << 32)
            | ((ulong)(shader.ID & 0xFFFF) << 16)
            | (ulong)(texture.ID & 0xFFFF);
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
