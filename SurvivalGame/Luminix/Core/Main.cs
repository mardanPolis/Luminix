using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Luminix.Rendering;
using Luminix.Resources;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Core.InputHandlers;
using Luminix.Resources.AssetHandlers;
using System.IO;
using System.Reflection;
using Luminix.IO;
using Luminix.Resources.BuiltIn;

namespace Luminix;
public class Main : Microsoft.Xna.Framework.Game
{
    public const string VERSION = "v1.0";
    public Main()
    {
        //workspace
        workspace = Workspace.Create();
        embeddedIO = new EmbeddedIO();

        logger = Logger.Get();
        logger.Log($"Started initialization");

        Settings.Initialize(out Settings.Controller settingsController);
        settingsController.Debug = true;
        settingsController.DebugAssets = true;
        settingsController.DebugInitialization = true;

        settingsController.MinWorkerThreadCount = 2;
        settingsController.MaxWorkerThreadCount = 5;

        stopwatch = Stopwatch.StartNew();
        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
        graphics.IsFullScreen = false;

        resourceManager = new ResourceManager(this);
        IsMouseVisible = true;

        ImmediateRenderer.ResourceManager = resourceManager;

        //initialize thread bridges
        CoreToRender.Initialize();
        RenderToCore.Initialize();

        //threads
        int workerThreadCount = Math.Max(Settings.MinWorkerThreadCount, Environment.ProcessorCount - 2);
        coreThread = new CoreThread(workerThreadCount);

        //starting threads
        coreThread.Start();
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
    private EmbeddedIO embeddedIO;
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
        try
        {
            startBytes = GC.GetAllocatedBytesForCurrentThread();
            
            ThreadPool.SetMaxThreads(3, 1);

            //systems
            renderingSystem = new RenderingSystem(this);

            //handlers
            inputHandler = new InputHandler();
            assetRegistrationHandler = new AssetRegistrationHandler(CoreToRender.AssetBridge, resourceManager);
            
            //threads
            coreThread.Priority = ThreadPriority.AboveNormal;

            //base initialization
            base.Initialize();

            TextureAsset nullTextureAsset = new ("null", BuiltInResources.NullTextureColor, new Vector2(32, 32));
            resourceManager.CreateResource(nullTextureAsset);

            //Loading Embedded assets
            foreach(var asset in embeddedIO.EnumerateAssets(Assembly.GetExecutingAssembly(), silent:true))
            {
                try
                {
                    if(Settings.DebugInitialization && Settings.DebugAssets)
                        logger.Log($"Loading embedded asset: {asset.Name}");
                    resourceManager.CreateResource(asset);
                }
                catch(Exception ex)
                {
                    logger.Err($"Failed to load embedded asset '{asset.Name}', reason: {ex}");
                }
            }
        
            //Renderer initialization   
            renderingSystem.Initialize();

            TargetElapsedTime = TimeSpan.FromSeconds(1d / 30d);
            IsFixedTimeStep = true;


        }
        catch
        {
            Logger.Get().Err($"Exception happened during initialization");
            throw;
        }
        workspace.SetState(Workspace.State.Running);

        logger.Log($"RenderThread initialized in {stopwatch.Elapsed.Milliseconds}ms");

        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;

        logger.Log($"RenderThread allocated {allocated / 1024.0:F2} KB ({allocated:N0}) bytes during initialization!");
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
}
