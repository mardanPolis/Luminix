using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Game;
using Luminix.Collections;
using Luminix.Rendering;
using Luminix.Resources;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Core.InputHandlers;
using Luminix.Resources.AssetHandlers;

namespace Luminix;
public class Main : Microsoft.Xna.Framework.Game
{
    public const string VERSION = "1.0v";
    public const string ENGINE_PACKAGE_ASSET_EXTENSION = ".lxpkg";

    public Main()
    {
        graphics = new GraphicsDeviceManager(this);
        resourceManager = new ResourceManager(this);
        IsMouseVisible = true;
    }

    private Thread mainThread = Thread.CurrentThread;
    private CoreThread coreThread = null;

    private long startBytes = 0;
    private long allocated = 0;
    private double frameTime;

    private Stopwatch stopwatch;
    private GameTime time;
    private Logger logger;

    private Workspace workspace;
    private RenderingSystem renderingSystem;

    private ResourceManager resourceManager;
    private GraphicsDeviceManager graphics;

    private InputHandler inputHandler;

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

    internal ResourceManager ResourceManager => resourceManager;
    internal GraphicsDeviceManager GraphicsDeviceManager => graphics;

    private AssetRegistrationHandler assetRegistrationHandler;

    protected override void Initialize()
    {
        stopwatch = Stopwatch.StartNew();
        startBytes = GC.GetAllocatedBytesForCurrentThread();
        
        logger = Logger.Get();
        logger.Log($"Started initialization");

        Settings.Initialize(out Settings.Controller settingsController);
        settingsController.Debug = true;
        settingsController.DebugAssets = true;
        settingsController.DebugInitialization = true;

        try
        {
            //workspace
            workspace = Workspace.Create();

            //threads
            int workerThreadCount = Math.Max(Settings.MinWorkerThreadCount, Environment.ProcessorCount - 2);
            coreThread = new CoreThread(workerThreadCount);

            //systems
            renderingSystem = new RenderingSystem(this);

            //initialize thread bridges
            CoreToRender.Initialize();
            RenderToCore.Initialize();

            //handlers
            inputHandler = new InputHandler();
            assetRegistrationHandler = new AssetRegistrationHandler(CoreToRender.AssetBridge, resourceManager);
            
            //threads
            coreThread.Priority = ThreadPriority.AboveNormal;

            //base initialization
            base.Initialize();

            //Renderer initialization   
            renderingSystem.Initialize();

            TargetElapsedTime = TimeSpan.FromSeconds(1d / 30d);
            IsFixedTimeStep = true;

            //starting threads
            coreThread.Start();

        }
        catch
        {
            Logger.Get().Err($"Exception happened during initialization");
            throw;
        }

        logger.Log($"Successful initialization");
        
        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;

        logger.Log($"RenderThread initialized in {stopwatch.Elapsed.Milliseconds}ms");
        logger.Log($"RenderThread allocated {allocated} bytes during initialization!");

        workspace.SetState(Workspace.State.Running);
    }
    protected override void Update(GameTime gameTime)
    {
        stopwatch.Restart();
        startBytes = GC.GetAllocatedBytesForCurrentThread();

        inputHandler.Update(IsActive);
        assetRegistrationHandler.Run();

        RenderToCore.WindowIsActive = IsActive;

        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        renderingSystem.DrawFrame(coreThread.PartialTick);
        base.Draw(gameTime);

        frameTime = stopwatch.Elapsed.TotalNanoseconds;
        time = gameTime;
        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        coreThread.Stop();
        //save current world
    }
}
