using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Luminix.Rendering;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.InputSystem.Handlers;
using Luminix.Threading.Buffers;
using Luminix.Threading.Commands;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;
internal sealed class MainRender : Microsoft.Xna.Framework.Game
{
    internal MainRender()
    {
        //workspace
        workspace = new Workspace();

        stopwatch = Stopwatch.StartNew();

        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = true;
        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
        graphics.IsFullScreen = false;
        graphics.HardwareModeSwitch = false;
        graphics.PreferHalfPixelOffset = false;

        resourceManager = new GraphicsResourceManager(this);
        IsMouseVisible = true;
    }

    //Core Bridges
    private TripleBufferBridge<RenderBuffer> renderBridge;
    private DoubleBufferBridge<RenderInfoBuffer> renderInfoBridge;
    private DoubleBufferBridge<InputBuffer> inputBridge;
    private CommandBridge<GraphicResourceCommand> resourceBridge;
    private WindowBridge windowBridge;

    //RenderThread
    private Thread mainThread;
    private TickedThread coreThread;

    private volatile bool shouldStop = false;

    private long startBytes;
    private long allocated;
    private double frameTime;

    private Point windowPosition;

    private Stopwatch stopwatch;
    private GameTime time;

    private Workspace workspace;
    private RenderingSystem renderingSystem;

    private GraphicsResourceManager resourceManager;
    private GraphicsDeviceManager graphics;

    private InputHandler inputHandler;

    private RenderInfoBuffer renderInfoBuffer;

    private Action<WindowCommand> windowCommandHandler;

    public Thread MainThread => mainThread;
    public TickedThread CoreThread => coreThread;

    internal GraphicsResourceManager ResourceManager => resourceManager;
    internal GraphicsDeviceManager GraphicsDeviceManager => graphics;

    public double FrameTime => frameTime;
    public double FrameDeltaTime => time != null ? time.ElapsedGameTime.TotalSeconds : 0;
    public double FPS => FrameDeltaTime > 0 ? 1.0 / FrameDeltaTime : double.MaxValue;
    public long FrameRate => (long)(1.0 / TargetElapsedTime.TotalSeconds);
    public long FrameAllocationAmount => allocated;

    public GameTime GameTime => time;


    public new ContentManager Content
    {
        init => base.Content = value;
    }

    public void Stop()
    {
        shouldStop = true;
    }

    protected override void Initialize()
    {
        try
        {
            var adapters = GraphicsAdapter.Adapters;

            this.mainThread = Thread.CurrentThread;
            this.coreThread = (TickedThread)Thread.GetThreadByName(Application.CORE_THREAD_NAME);

            Logger.NewLog(Logger.Log.Level.Info, $"Started initialization");

            //thread bridges
            renderBridge            = ThreadBridges.GetBridge<TripleBufferBridge<RenderBuffer>>(Application.CORE_THREAD_NAME);
            renderInfoBridge        = ThreadBridges.GetBridge<DoubleBufferBridge<RenderInfoBuffer>>(Application.CORE_THREAD_NAME);
            inputBridge             = ThreadBridges.GetBridge<DoubleBufferBridge<InputBuffer>>(Application.CORE_THREAD_NAME);
            resourceBridge          = ThreadBridges.GetBridge<CommandBridge<GraphicResourceCommand>>(Application.CORE_THREAD_NAME);
            windowBridge            = ThreadBridges.GetBridge<WindowBridge>(Application.CORE_THREAD_NAME);
            
            resourceManager.Initialize();

            inputBridge.PopulateIfEmpty();

            startBytes = GC.GetAllocatedBytesForCurrentThread();

            //systems
            renderingSystem = new RenderingSystem(this, renderBridge);

            //handlers
            inputHandler = new InputHandler(inputBridge);
            
            windowBridge.ConsumeCreateCommands((command) =>
            {
                Window.Title = command.Title;
                if(command.Position.HasValue) Window.Position = command.Position.Value;
                if(command.WindowMode.HasValue) renderingSystem.SetWindowMode(command.WindowMode.Value);

                var response = new WindowCreateCommandResponse
                {
                    Handle = Window.Handle,
                    Position = Window.Position,
                    WindowMode = renderingSystem.IsFullScreen ? Luminix.Window.Mode.Fullscreen : Luminix.Window.Mode.Windowed
                };
                return response;
            });

            base.Initialize();

            windowCommandHandler = (command) =>
            {
                switch(command.Kind)
                {
                    case WindowCommandType.SetTitle:
                        Window.Title = command.Title;
                        break;
                    case WindowCommandType.SetPosition:
                        Window.Position = command.Position;
                        break;
                    case WindowCommandType.SetMode:
                        renderingSystem.SetWindowMode(command.WindowMode);
                        break;
                }
            };

            TargetElapsedTime = TimeSpan.FromSeconds(1d / 144d);
            IsFixedTimeStep = true;

            windowPosition = Window.Position;

            workspace.Start();
        }
        catch
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Exception happened during initialization");
            throw;
        }

        Logger.NewLog(Logger.Log.Level.Info, $"RenderThread initialized in {stopwatch.Elapsed.Milliseconds}ms");

        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;

        Logger.NewLog(Logger.Log.Level.Info, $"RenderThread allocated {allocated / 1024.0:F2} KB | ({allocated:N0} bytes) during initialization!");
    }

    protected override void Update(GameTime gameTime)
    {
        if(shouldStop)
            Exit();

        stopwatch.Restart();
        renderInfoBuffer = renderInfoBridge.WriteBuffer ?? renderInfoBridge.GetNewBuffer();

        inputHandler.Update(IsActive);


        if(windowBridge.CommandsIncoming)
        {
            windowBridge.ConsumeCommands(windowCommandHandler);
            windowBridge.ClearSyncCommands();
        }

        SendWindowSyncCommand();

        base.Update(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        Application.Exit();
        base.OnExiting(sender, args);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        float partialTick = coreThread.PartialTick;

        renderingSystem.NewFrame(partialTick);

        base.Draw(gameTime);

        frameTime = stopwatch.Elapsed.TotalSeconds;
        time = gameTime;
        
        renderInfoBuffer.Allocated += allocated;
        renderInfoBuffer.DrawCalls = renderingSystem.DrawCount;
        renderInfoBuffer.FrameCount = renderingSystem.FrameCount;
        renderInfoBuffer.AccumulatedFrameCount++;
        renderInfoBuffer.FrameDeltaTime += time.ElapsedGameTime.TotalSeconds;
        renderInfoBuffer.FrameTime += frameTime;

        renderInfoBuffer.Updated = true;
        
        allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        startBytes = GC.GetAllocatedBytesForCurrentThread();
    }
    
    private void SendWindowSyncCommand()
    {
        var command = new WindowSyncCommand
        {
            Handle = Window.Handle,
            Position = Window.Position,
            ClientBound = Window.ClientBounds,
            IsFocused = IsActive,
            IsMinimized = Window.IsMinimized,
            CurrentDisplay = Window.GetDisplayIndex()
        };

        windowBridge.NewCommand(command);
    }
}
