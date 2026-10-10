using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using System.Runtime.CompilerServices;
using Luminix.Rendering;
using Luminix.Threading.Commands;

namespace Luminix;

public sealed class Graphics : Workspace.Service
{
    public class Resolution
    {        
        internal Resolution(Point point, int refreshRate)
        {
            Point = point;
            RefreshRate = refreshRate;
        }

        internal Point Point { get; }
        public int X => Point.X;
        public int Y => Point.Y;
        public int RefreshRate { get; }
    }

    internal Graphics()
    {
        renderBridge = ThreadBridges.GetBridge<TripleBufferBridge<RenderBuffer>>(Application.RENDER_THREAD_NAME);
        renderInfoBridge = ThreadBridges.GetBridge<DoubleBufferBridge<RenderInfoBuffer>>(Application.RENDER_THREAD_NAME);
        windowBridge = ThreadBridges.GetBridge<WindowBridge>(Application.RENDER_THREAD_NAME);

        syncWindowCommandHandler = (command) =>
        {
            var window = this.window;
            if(command.Handle != window.Handle)
                return;
            
            this.windowPosition = command.Position;
            
            window.Position = command.Position;
            window.ClientBounds = command.ClientBound;
            window.IsFocused = command.IsFocused;
            window.IsMinimized = command.IsMinimized;
            window.CurrentDisplay = command.CurrentDisplay;
        };

        var adapters = GraphicsAdapter.Adapters;

        Screen[] screens = new Screen[adapters.Count];

        for(int adapterIndex = 0; adapterIndex < adapters.Count; adapterIndex++)
            screens[adapterIndex] = new Screen(adapters[adapterIndex]);

        this.screens = screens;
        renderSettings.Resolution = screens[0].Resolutions[0];

        window = new Window();
        var createCommand = new WindowCreateCommand();
        createCommand.Title = Application.Name;
         // TODO : load settings.
        createCommand.Position = null;
        createCommand.WindowMode = null;

        var task = windowBridge.NewCommand(createCommand);
        var result = task.GetAwaiter().GetResult().Item2;
        window.Handle = result.Handle;

        windowTitle = createCommand.Title;
        windowPosition = result.Position;
        windowMode = result.WindowMode;

        window.Title = createCommand.Title;
        window.Position = result.Position;
        window.CurrentMode = result.WindowMode;
    }
    
    private RenderBuffer renderBuffer;

    private readonly TripleBufferBridge<RenderBuffer> renderBridge;
    private readonly DoubleBufferBridge<RenderInfoBuffer> renderInfoBridge;
    private readonly WindowBridge windowBridge;

    private readonly Action<WindowSyncCommand> syncWindowCommandHandler;

    private Screen[] screens;
    private Window window;

    private string windowTitle;
    private Point windowPosition;
    private Window.Mode windowMode;

    private Viewport viewport;
    private FrameInfo frameInfo;
    private RenderSettings renderSettings;

    private EntityID mainCamera;

    public ref readonly Viewport Viewport => ref viewport;
    public ref readonly FrameInfo FrameInfo => ref frameInfo;
    public ref RenderSettings RenderSettings => ref renderSettings;

    public EntityID MainCamera 
    { 
        get => mainCamera; 
        set
        {
            var world = Workspace.GetService<World>();
            if(!world.EntityExists(value))
            {
                Logger.NewLog(Logger.Log.Level.Warning, $"Cant use entity with ID '{value.Value}', reason: entity with that ID does not exist.");
                return;
            }

            var entityType = world.GetEntityType(value);
            if(entityType.HasComponent<Camera>())
            {
                Logger.NewLog(Logger.Log.Level.Warning, $"Cant use entity with ID '{value.Value}', reason: entity does not have 'Camera' component.");
                return;
            }

            mainCamera = value;
        } 
    }

    public bool IsRendering => renderBuffer != null;


    public void DrawSprite(TextureResource texture, in Vector3 position)                        => DrawSprite(null, texture, Color.White, new Transform(position));
    public void DrawSprite(TextureResource texture, in Vector3 position, float rotation)        => DrawSprite(null, texture, Color.White, new Transform(position, rotation));
    public void DrawSprite(TextureResource texture, in Vector3 position, in Vector2 scale)      => DrawSprite(null, texture, Color.White, new Transform(position, scale));
    public void DrawSprite(TextureResource texture, in Transform transform)                     => DrawSprite(null, texture, Color.White, in transform);
    public void DrawSprite(TextureResource texture, Color color, in Transform transform)     => DrawSprite(null, texture, color, in transform);
    public void DrawSprite(ShaderResource shader, TextureResource texture, Color color, in Transform transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushSprite(shader, texture, color, in transform);
    }

    public void DrawText(FontResource font, string text, in Vector3 position)                       => DrawText(null, font, text, Color.White, new Transform(position));
    public void DrawText(FontResource font, string text, in Vector3 position, float rotation)       => DrawText(null, font, text, Color.White, new Transform(position, rotation));
    public void DrawText(FontResource font, string text, in Vector3 position, Vector2 scale)        => DrawText(null, font, text, Color.White, new Transform(position, scale));
    public void DrawText(FontResource font, string text, in Transform transform)                    => DrawText(null, font, text, Color.White, in transform);
    public void DrawText(FontResource font, string text, Color color, in Transform transform)    => DrawText(null, font, text, color, in transform);
    public void DrawText(ShaderResource shader, FontResource font, string text, Color color, in Transform transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushText(shader, font, color, text, in transform);
    }


    public void DrawSprite(TextureResource texture, in Vector3 position0, in Vector3 position)                                      => DrawSprite(null, texture, Color.White, new TransformInterp(position0, position));
    public void DrawSprite(TextureResource texture, in Vector3 position0, in Vector3 position, float rotation0, float rotation)     => DrawSprite(null, texture, Color.White, new TransformInterp(position0, rotation0, position, rotation));
    public void DrawSprite(TextureResource texture, in Vector3 position0, in Vector3 position, in Vector2 scale0, Vector2 scale)    => DrawSprite(null, texture, Color.White, new TransformInterp(position0, scale0, position, scale));
    public void DrawSprite(TextureResource texture, in TransformInterp transform)                                                   => DrawSprite(null, texture, Color.White, in transform);
    public void DrawSprite(TextureResource texture, Color color, in TransformInterp transform)                                      => DrawSprite(null, texture, color, in transform);
    public void DrawSprite(ShaderResource shader, TextureResource texture, Color color, in TransformInterp transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushInterpolatedSprite(shader, texture, color, in transform);
    }
    public void DrawText(FontResource font, string text, in Vector3 position0, in Vector3 position)                                     => DrawText(null, font, text, Color.White, new TransformInterp(position0, position));
    public void DrawText(FontResource font, string text, in Vector3 position0, in Vector3 position, float rotation0, float rotation)    => DrawText(null, font, text, Color.White, new TransformInterp(position0, rotation0, position, rotation));
    public void DrawText(FontResource font, string text, in Vector3 position0, in Vector3 position, Vector2 scale0, Vector2 scale)      => DrawText(null, font, text, Color.White, new TransformInterp(position0, scale0, position, scale));
    public void DrawText(FontResource font, string text, in TransformInterp transform)                                                  => DrawText(null, font, text, Color.White, in transform);
    public void DrawText(FontResource font, string text, Color color, in TransformInterp transform)                                     => DrawText(null, font, text, color, in transform);
    public void DrawText(ShaderResource shader, FontResource font, string text, Color color, in TransformInterp transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushInterpolatedText(shader, font, color, text, in transform);
    }

    public void DrawImage(TextureResource image, int x, int y, int width, int height)                               => DrawImage(null, image, Color.White, x, y, width, height, 0);
    public void DrawImage(TextureResource image, int x, int y, int width, int height, float rotation)               => DrawImage(null, image, Color.White, x, y, width, height, rotation);
    public void DrawImage(TextureResource image, Color color, int x, int y, int width, int height, float rotation)  => DrawImage(null, image, color, x, y, width, height, rotation);
    public void DrawImage(ShaderResource shader, TextureResource image, Color color, int x, int y, int width, int height, float rotation)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushImage(shader, image, color, x, y, width, height, rotation);
    }

    public void DrawLabel(FontResource font, string text, int x, int y)                                                     => DrawLabel(null, font, text, Color.White, x, y, 0, 0, 0);
    public void DrawLabel(FontResource font, string text, int x, int y, int width, int height)                              => DrawLabel(null, font, text, Color.White, x, y, width, height, 0);
    public void DrawLabel(FontResource font, string text, int x, int y, int width, int height, float rotation)              => DrawLabel(null, font, text, Color.White, x, y, width, height, rotation);
    public void DrawLabel(FontResource font, string text, Color color, int x, int y, int width, int height, float rotation) => DrawLabel(null, font, text, color, x, y, width, height, rotation);
    public void DrawLabel(ShaderResource shader, FontResource font, string text, Color color, int x, int y, int width, int height, float rotation)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushLabel(shader, font, color, text, x, y, width, height, rotation);
    }

    public Screen GetScreen() => screens[window.CurrentDisplay];
    public Window GetWindow() => window;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EnsureRenderBuffer([CallerMemberName] string caller = "")
    {
        if (renderBuffer == null)
            throw new InvalidOperationException($"{caller} must be called during rendering.");
        return true;
    }


    internal void SwapBuffers()
    {
        this.renderBridge.Swap();
        this.renderInfoBridge.Swap();

        var renderInfoBuffer = this.renderInfoBridge.ReadBuffer;
        if(renderInfoBuffer != null && renderInfoBuffer.Updated)
        {
            var accumulatedFrameCount = renderInfoBuffer.AccumulatedFrameCount;
            ref FrameInfo frameInfo = ref this.frameInfo;
            frameInfo = new FrameInfo(
                renderInfoBuffer.FrameCount,
                renderInfoBuffer.FrameTime / accumulatedFrameCount,
                renderInfoBuffer.FrameDeltaTime / accumulatedFrameCount,
                renderInfoBuffer.Allocated,
                renderInfoBuffer.DrawCalls
            );
            renderInfoBuffer.Reset();
        }

        var windowTitle = window.Title;
        var windowPosition = window.Position;
        var windowMode = window.CurrentMode;

        if(windowTitle != this.windowTitle)
        {
            var command = new WindowCommand
            {
                Kind = WindowCommandType.SetTitle,
                Title = windowTitle,
            };
            this.windowTitle = windowTitle;
            windowBridge.NewCommand(command);
            windowBridge.ClearSyncCommands();
        }

        if(windowPosition != this.windowPosition)
        {
            var command = new WindowCommand
            {
                Kind = WindowCommandType.SetPosition,
                Position = windowPosition,
            };
            this.windowPosition = windowPosition;
            windowBridge.NewCommand(command);
            windowBridge.ClearSyncCommands();
        }

        if(windowMode != this.windowMode)
        {
            var command = new WindowCommand
            {
                Kind = WindowCommandType.SetMode,
                WindowMode = windowMode,
            };
            this.windowMode = windowMode;
            windowBridge.NewCommand(command);
            windowBridge.ClearSyncCommands();
        }

        windowBridge.ConsumeSyncCommand(syncWindowCommandHandler);
    }


    internal void BeginRender()
    {
        renderBuffer = renderBridge.WriteBuffer ?? renderBridge.GetNewBuffer();
        renderBuffer.Reset();
    }

    internal void StopRender()
    {
        ref RenderSettings settings = ref this.RenderSettings;
        var renderBuffer = this.renderBuffer;

        var world = this.Workspace.GetService<World>();
        if(world.EntityExists(this.mainCamera))
            renderBuffer.SetCam(world.GetEntityReadonlyComponent<Camera>(this.mainCamera));
        else
            renderBuffer.SetCam(null);
            
        renderBuffer.RenderSettings = this.renderSettings;

        this.renderBuffer = null;
    }
}