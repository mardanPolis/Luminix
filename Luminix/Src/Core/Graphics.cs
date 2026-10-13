using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using System.Runtime.CompilerServices;
using Luminix.Rendering;
using Luminix.Threading.Commands;
using System.Text;

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

        public override string ToString() => $"{X}, {Y}";
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
        
        var adapters = GraphicsAdapter.Adapters;

        Screen[] screens = new Screen[adapters.Count];

        for(int adapterIndex = 0; adapterIndex < adapters.Count; adapterIndex++)
        {
            screens[adapterIndex] = new Screen(adapters[adapterIndex]);
        }

        this.screens = screens;
        renderSettings.Resolution = screens[0].Resolutions[2];
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

    public FontResource DefaultFont { get; set; }

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
            if(!entityType.HasComponent<Camera>())
            {
                Logger.NewLog(Logger.Log.Level.Warning, $"Cant use entity with ID '{value.Value}', reason: entity does not have 'Camera' component.");
                return;
            }

            mainCamera = value;
        } 
    }

    public bool IsRendering => renderBuffer != null;

    public void DrawSprite(TextureResource texture, in Transform transform)                                     => DrawSprite(new ShaderID(0), texture.Source, texture.Rectangle, Color.White, in transform);
    public void DrawSprite(TextureResource texture, Color color, in Transform transform)                        => DrawSprite(new ShaderID(0), texture.Source, texture.Rectangle, color, transform);
    public void DrawSprite(ShaderResource shader, TextureResource texture, Color color, in Transform transform) => DrawSprite(shader.Source, texture.Source, texture.Rectangle, color, transform);
    private void DrawSprite(ShaderID shader, TextureID texture, in Rectangle rectangle, Color color, in Transform transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushSprite(shader, texture, rectangle, color, in transform);
    }

    public void DrawSprite(TextureResource texture, in TransformInterp transform)                                                   => DrawSprite(new ShaderID(0), texture.Source, texture.Rectangle, Color.White, in transform);
    public void DrawSprite(TextureResource texture, Color color, in TransformInterp transform)                                      => DrawSprite(new ShaderID(0), texture.Source, texture.Rectangle, color, transform);
    public void DrawSprite(ShaderResource shader, TextureResource texture, Color color, in TransformInterp transform)               => DrawSprite(shader.Source, texture.Source, texture.Rectangle, color, transform);
    private void DrawSprite(ShaderID shader, TextureID texture, in Rectangle rectangle, Color color, in TransformInterp transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushInterpolatedSprite(shader, texture, rectangle, color, in transform);
    }

    public void DrawImage(TextureResource image, Point position, Point size)                                                        => DrawImage(new ShaderID(0), image.Source, image.Rectangle, Color.White, position, size, 0);
    public void DrawImage(TextureResource image, Point position, Point size, float rotation)                                        => DrawImage(new ShaderID(0), image.Source, image.Rectangle, Color.White, position, size, rotation);
    public void DrawImage(TextureResource image, Point position, float rotation)                                                    => DrawImage(new ShaderID(0), image.Source, image.Rectangle, Color.White, position, new Point(1, 1), rotation);
    public void DrawImage(TextureResource image, Color color, Point position, Point size, float rotation)                           => DrawImage(new ShaderID(0), image.Source, image.Rectangle, color, position, size, rotation);
    public void DrawImage(ShaderResource shader, TextureResource image, Color color, Point position, Point size, float rotation)    => DrawImage(shader.Source, image.Source, image.Rectangle, color, position, size, rotation);
    private void DrawImage(ShaderID shader, TextureID image, Rectangle rectangle, Color color, Point position, Point size, float rotation)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushImage(shader, image, rectangle, color, position, size, rotation);
    }




    public void DrawLabel(StringBuilder text, Point position, Vector2? alignment = null, bool fitToBox = false)                                                                 => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, null, 0, alignment, fitToBox);
    public void DrawLabel(StringBuilder text, Point position, Point size, Vector2? alignment = null, bool fitToBox = false)                                                     => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, size, 0, alignment, fitToBox);
    public void DrawLabel(StringBuilder text, Point position, float rotation, Vector2? alignment = null, bool fitToBox = false)                                                 => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, null, rotation, alignment, fitToBox);
    public void DrawLabel(StringBuilder text, Point position, Point size, float rotation, Vector2? alignment = null, bool fitToBox = false)                                     => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(Color color, StringBuilder text, Point position, Point? size, float rotation, Vector2? alignment = null, bool fitToBox = false)                       => DrawLabel(new ShaderID(0), null, new Text(text), color, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(FontResource font, StringBuilder text, Point position, Vector2? alignment = null, bool fitToBox = false)                                              => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, null, 0, alignment, fitToBox);
    public void DrawLabel(FontResource font, StringBuilder text, Point position, Point size, Vector2? alignment = null, bool fitToBox = false)                                  => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, size, 0, alignment, fitToBox);
    public void DrawLabel(FontResource font, StringBuilder text, Point position, float rotation, Vector2? alignment = null, bool fitToBox = false)                              => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, null, rotation, alignment, fitToBox);
    public void DrawLabel(FontResource font, StringBuilder text, Point position, Point size, float rotation, Vector2? alignment = null, bool fitToBox = false)                  => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(FontResource font, StringBuilder text, Color color, Point position, Point size, float rotation, Vector2? alignment = null, bool fitToBox = false)     => DrawLabel(new ShaderID(0), font, new Text(text), color, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(ShaderResource shader, FontResource font, StringBuilder text, Color color, Point position, Point? size, float rotation, Vector2? alignment = null, bool fitToBox = false) => DrawLabel(shader.Source, font, new Text(text), color, position, size, rotation, alignment, fitToBox);

    public void DrawLabel(ReadOnlySpan<char> text, Point position, Vector2? alignment = null, bool fitToBox = false)                                                             => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, null, 0, alignment, fitToBox);
    public void DrawLabel(ReadOnlySpan<char> text, Point position, Point size, Vector2? alignment = null, bool fitToBox = false)                                                 => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, size, 0, alignment, fitToBox);
    public void DrawLabel(ReadOnlySpan<char> text, Point position, float rotation, Vector2? alignment = null, bool fitToBox = false)                                             => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, null, rotation, alignment, fitToBox);
    public void DrawLabel(ReadOnlySpan<char> text, Point position, Point size, float rotation, Vector2? alignment = null, bool fitToBox = false)                                 => DrawLabel(new ShaderID(0), null, new Text(text), Color.White, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(Color color, ReadOnlySpan<char> text, Point position, Point? size, float rotation, Vector2? alignment = null, bool fitToBox = false)                   => DrawLabel(new ShaderID(0), null, new Text(text), color, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(FontResource font, ReadOnlySpan<char> text, Point position, Vector2? alignment = null, bool fitToBox = false)                                          => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, null, 0, alignment, fitToBox);
    public void DrawLabel(FontResource font, ReadOnlySpan<char> text, Point position, Point size, Vector2? alignment = null, bool fitToBox = false)                              => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, size, 0, alignment, fitToBox);
    public void DrawLabel(FontResource font, ReadOnlySpan<char> text, Point position, float rotation, Vector2? alignment = null, bool fitToBox = false)                          => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, null, rotation, alignment, fitToBox);
    public void DrawLabel(FontResource font, ReadOnlySpan<char> text, Point position, Point size, float rotation, Vector2? alignment = null, bool fitToBox = false)              => DrawLabel(new ShaderID(0), font, new Text(text), Color.White, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(FontResource font, ReadOnlySpan<char> text, Color color, Point position, Point size, float rotation, Vector2? alignment = null, bool fitToBox = false) => DrawLabel(new ShaderID(0), font, new Text(text), color, position, size, rotation, alignment, fitToBox);
    public void DrawLabel(ShaderResource shader, FontResource font, ReadOnlySpan<char> text, Color color, Point position, Point? size, float rotation, Vector2? alignment = null, bool fitToBox = false) => DrawLabel(shader.Source, font, new Text(text), color, position, size, rotation, alignment, fitToBox);

    private void DrawLabel(ShaderID shader, FontResource font, Text text, Color color, Point position, Point? size, float rotation, Vector2? alignment = null, bool fitToBox = false)
    {
        font ??= DefaultFont;
        if (font == null || text.Length == 0) return;
        if (!EnsureRenderBuffer()) return;

        Vector2 textSize = font.Measure(text);
        Vector2 boxSize = size.HasValue ? new Vector2(size.Value.X, size.Value.Y) : textSize;
        Vector2 align = alignment ?? new Vector2(0.5f, 0.5f);

        float fit = 1f;
        if (fitToBox && size.HasValue && textSize.X > 0f && textSize.Y > 0f)
            fit = MathF.Min(boxSize.X / textSize.X, boxSize.Y / textSize.Y);

        Vector2 textOrigin = -boxSize * 0.5f + (boxSize - textSize * fit) * align;

        Vector2 boxCenter = new Vector2(position.X, position.Y) + boxSize * 0.5f;

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        foreach (var glyph in font.Enumerate(text))
        {
            Vector2 local = textOrigin + glyph.Center * fit;

            var glyphPosition = new Point(
                (int)MathF.Round(boxCenter.X + local.X * cos - local.Y * sin),
                (int)MathF.Round(boxCenter.Y + local.X * sin + local.Y * cos));

            var glyphSize = new Point(
                Math.Max(1, (int)MathF.Round(glyph.Texture.Width * fit)),
                Math.Max(1, (int)MathF.Round(glyph.Texture.Height * fit)));

            DrawImage(shader, glyph.Texture.Source, glyph.Texture.Rectangle, color, glyphPosition, glyphSize, rotation);
        }
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
    
    private void UpdateViewport()
    {
        var target = renderSettings.Resolution.Point;
        var bounds = window.ClientBounds;

        int targetWidth = target.X;
        int targetHeight = target.Y;
        int windowWidth = bounds.Width;
        int windowHeight = bounds.Height;

        if (windowWidth <= 0 || windowHeight <= 0 || targetWidth <= 0 || targetHeight <= 0)
            return;

        float targetAspect = (float)targetWidth / targetHeight;
        float windowAspect = (float)windowWidth / windowHeight;

        int width = windowWidth;
        int height = windowHeight;
        int x = 0;
        int y = 0;

        if (windowAspect > targetAspect)
        {
            width = (int)(windowHeight * targetAspect);
            x = (windowWidth - width) / 2;
        }
        else
        {
            height = (int)(windowWidth / targetAspect);
            y = (windowHeight - height) / 2;
        }

        viewport = new Viewport(x, y, width, height);
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
        UpdateViewport();

        renderBuffer = renderBridge.WriteBuffer ?? renderBridge.GetNewBuffer();
        renderBuffer.Reset();
    }
    internal void StopRender()
    {
        ref RenderSettings settings = ref this.RenderSettings;
        var renderBuffer = this.renderBuffer;
        renderBuffer.RenderSettings = settings;

        var world = this.Workspace.GetService<World>();
        if(world.EntityExists(this.mainCamera))
            renderBuffer.SetCam(world.GetEntityReadonlyComponent<Camera>(this.mainCamera));
        else
        {
            var text = "No camera detected!";
            var viewport = Viewport;
            var textSize = new Point(200, 200);
            var Center = new Point(
                viewport.Width / 2 - textSize.X / 2,
                viewport.Height / 2 - textSize.Y / 2
            );

            DrawLabel(text, Center, textSize, alignment:new Vector2(0.5f, 0.5f), fitToBox:true);
            renderBuffer.SetCam(null);
        }
            
        this.renderBuffer = null;
    }
}