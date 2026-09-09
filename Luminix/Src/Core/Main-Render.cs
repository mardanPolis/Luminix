using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Luminix.Rendering;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.InputSystem.Handlers;
using Luminix.Threading.Buffers;
using Luminix.IO;
using Luminix.Threading.Commands;
using System.Collections.Frozen;

namespace Luminix;
internal sealed class MainRender : Microsoft.Xna.Framework.Game
{
    internal MainRender()
    {
        //workspace
        workspace = new Workspace(out workspaceController);

        stopwatch = Stopwatch.StartNew();

        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = true;
        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
        graphics.IsFullScreen = false;
        graphics.HardwareModeSwitch = false;
        graphics.PreferHalfPixelOffset = false;

        resourceManager = new ResourceManager(this);
        IsMouseVisible = true;
    }

    //Core Bridges
    private TripleBufferBridge<RenderBuffer> renderBridge;
    private DoubleBufferBridge<RenderInfoBuffer> renderInfoBridge;
    private DoubleBufferBridge<InputBuffer> inputBridge;
    private ResourceBridge resourceBridge;
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
    private Workspace.Controller workspaceController;
    private RenderingSystem renderingSystem;

    private ResourceManager resourceManager;
    private GraphicsDeviceManager graphics;

    private InputHandler inputHandler;

    private RenderInfoBuffer renderInfoBuffer;

    private Action<ResourceCommand> resourceCommandHandler;
    private Action<WindowCommand> windowCommandHandler;

    public Thread MainThread => mainThread;
    public TickedThread CoreThread => coreThread;

    internal ResourceManager ResourceManager => resourceManager;
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
            this.mainThread = Thread.CurrentThread;
            this.coreThread = (TickedThread)Thread.GetThreadByName("CoreThread");

            Logger.NewLog(Logger.Log.Level.Info, $"Started initialization in {stopwatch.Elapsed.Milliseconds}ms");

            //thread bridges
            renderBridge            = ThreadBridges.GetBridge<TripleBufferBridge<RenderBuffer>>("CoreThread");
            renderInfoBridge        = ThreadBridges.GetBridge<DoubleBufferBridge<RenderInfoBuffer>>("CoreThread");
            inputBridge             = ThreadBridges.GetBridge<DoubleBufferBridge<InputBuffer>>("CoreThread");
            resourceBridge          = ThreadBridges.GetBridge<ResourceBridge>("CoreThread");
            windowBridge            = ThreadBridges.GetBridge<WindowBridge>("CoreThread");
            
            inputBridge.PopulateIfEmpty();

            startBytes = GC.GetAllocatedBytesForCurrentThread();
            
            AssetIO assetIO = new AssetIO();

            //systems
            renderingSystem = new RenderingSystem(this, renderBridge);

            //handlers
            inputHandler = new InputHandler(inputBridge);
            
            windowBridge.ConsumeCreateCommands(HandleWindowCreateCommand);

            //base initialization
            base.Initialize();

            TextureAsset nullTextureAsset = new ("NullTexture", Resources.BuiltIn.NullTextureColor, new Vector2(32, 32));
            TextureAsset pixelTextureAsset = new ("Pixel", Resources.BuiltIn.PixelTextureColor, new Vector2(1, 1));

            resourceManager.CreateResource(nullTextureAsset);
            resourceManager.CreateResource(pixelTextureAsset);

            resourceManager.CreateResource(assetIO.LoadXnbAsset(Application.Assembly, "Assets.Built-In.Fonts.IBMPlexMono-Bold.xnb"));
            resourceManager.CreateResource(assetIO.LoadXnbAsset(Application.Assembly, "Assets.Built-In.Fonts.IBMPlexMono-Medium.xnb"));
            resourceManager.CreateResource(assetIO.LoadXnbAsset(Application.Assembly, "Assets.Built-In.Fonts.IBMPlexMono-Regular.xnb"));
            resourceManager.CreateResource(assetIO.LoadXnbAsset(Application.Assembly, "Assets.Built-In.Shaders.GenericShader.xnb"));

            resourceCommandHandler = HandleResourceCommand;
            windowCommandHandler = HandleWindowCommand;

            //Renderer initialization   
            renderingSystem.Initialize();

            TargetElapsedTime = TimeSpan.FromSeconds(1d / 144d);
            IsFixedTimeStep = true;

            windowPosition = Window.Position;
        }
        catch
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Exception happened during initialization");
            throw;
        }

        workspaceController.Start();

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

        if(resourceBridge.ResourceCommandsIncoming)
        {
            resourceBridge.ConsumeCommands(resourceCommandHandler);
            resourceBridge.SetResourceContainer(resourceManager.Resources);
        }

        if(windowBridge.CommandsIncoming) // TODO : look into the window clunky syncing where 
        {
            windowBridge.ConsumeCommands(windowCommandHandler);
            windowBridge.ClearSyncCommands();
        }

        SendWindowSyncCommand();

        base.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        float partialTick = coreThread.PartialTick;

        renderingSystem.NewFrame(partialTick);

        base.Draw(gameTime);

        frameTime = stopwatch.Elapsed.TotalNanoseconds;
        time = gameTime;
        
        renderInfoBuffer.Allocated += allocated;
        renderInfoBuffer.DrawCalls = renderingSystem.DrawCount;
        renderInfoBuffer.FrameCount = renderingSystem.FrameCount;
        renderInfoBuffer.AccumulatedFrameCount++;
        renderInfoBuffer.FrameDeltaTime += time.ElapsedGameTime.TotalMilliseconds;
        renderInfoBuffer.FrameTime += frameTime;

        renderInfoBuffer.Updated = true;
        
        allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        startBytes = GC.GetAllocatedBytesForCurrentThread();
    }
    
    private void HandleResourceCommand(ResourceCommand command)
    {
        switch (command.Kind)
        {
            case ResourceCommand.Type.create:
                resourceManager.CreateResource(command.Asset);
            break;
            case ResourceCommand.Type.update:
                throw new NotImplementedException("update resource");
            case ResourceCommand.Type.unload:
                resourceManager.UnloadAsset(command.Asset.Identifier);
            break;
        }
    }

    private WindowCreateCommandResponse HandleWindowCreateCommand(WindowCreateCommand command)
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
    }

    private void HandleWindowCommand(WindowCommand command)
    {
        switch(command.Kind)
        {
            case WindowCommand.Type.SetTitle:
                Window.Title = command.Title;
            break;
            case WindowCommand.Type.SetPosition:
                Window.Position = command.Position;
            break;
            case WindowCommand.Type.SetMode:
                renderingSystem.SetWindowMode(command.WindowMode);
            break;
        }
    }

    private void SendWindowSyncCommand()
    {
        var command = new WindowSyncCommand
        {
            Handle = Window.Handle,
            Position = Window.Position,
            ClientBound = Window.ClientBounds,
            IsFocused = IsActive,
            IsMinimized = Window.IsMinimized
        };

        windowBridge.NewCommand(command);
    }
}
