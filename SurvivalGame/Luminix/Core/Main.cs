using System;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Rendering;
using Luminix.Resources;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Core.InputHandlers;
using Luminix.Resources.AssetHandlers;
using Luminix.IO;
using Luminix.Resources.BuiltIn;

namespace Luminix;
public class Main : Microsoft.Xna.Framework.Game
{
    public static string Version { get; private set; }
    public static string BuildDate { get; private set; }

    public Main()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string version = assembly.GetName().Version.ToString();

        var attribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                            .FirstOrDefault(a => a.Key == "BuildDate");
        string buildDate = attribute?.Value ?? "Unknown";

        Main.Version = version;
        Main.BuildDate = buildDate;

        //workspace
        workspace = Workspace.Create();
        embeddedIO = new EmbeddedIO();

        logger = Logger.Get();
        logger.Log($"Started initialization");
        logger.Log($"Build-Vers: {version}");
        logger.Log($"Build-Date: {buildDate}");

        Settings.Initialize(out Settings.Controller settingsController);
        settingsController.Debug = true;
        settingsController.DebugAssets = true;
        settingsController.DebugInitialization = true;

        Settings.DebugInfo = true;

        settingsController.MinWorkerThreadCount = 2;
        settingsController.MaxWorkerThreadCount = 5;

        var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

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

    private Point windowPosition;

    private Stopwatch stopwatch;
    private GameTime time;
    private Logger logger;

    private Workspace workspace;
    private EmbeddedIO embeddedIO;
    private RenderingSystem renderingSystem;

    private ResourceManager resourceManager;
    private GraphicsDeviceManager graphics;

    private InputHandler inputHandler;

    public Thread MainSystemThread => mainThread;
    public CoreThread CoreThread => coreThread;

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

    private bool[] functionKeys = { false, false, false, false, false, false, false };
    private Action[] keyActions = new Action[7];
    
    private bool takeScreenshot = false;

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

            TextureAsset nullTextureAsset = new ("NullTexture", BuiltInResources.NullTextureColor, new Vector2(32, 32));
            TextureAsset pixelTextureAsset = new ("Pixel", BuiltInResources.PixelTextureColor, new Vector2(1, 1));
            resourceManager.CreateResource(nullTextureAsset);
            resourceManager.CreateResource(pixelTextureAsset);

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

            TargetElapsedTime = TimeSpan.FromSeconds(1d / 144d);
            IsFixedTimeStep = true;

            keyActions[0] = () => Settings.DebugInfo = !Settings.DebugInfo;                                                 //F1
            keyActions[1] = () => Settings.DebugConsole = !Settings.DebugConsole;                                           //F2
            keyActions[2] = () => Settings.DebugInspector = !Settings.DebugInspector;                                       //F3
            keyActions[3] = () => Settings.DebugGizmosPanel = !Settings.DebugGizmosPanel;                                   //F4
            keyActions[4] = () => Settings.DebugEventPanel = !Settings.DebugEventPanel;                                     //F5
            keyActions[5] = () => { renderingSystem.Fullscreen(!renderingSystem.IsFullScreen); renderingSystem.Apply(); };  //F11
            keyActions[6] = () => takeScreenshot = true;                                                                    //F12

            windowPosition = Window.ClientBounds.Location;
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
        inputHandler.Update(IsActive);
        assetRegistrationHandler.Run();

        RenderToCore.WindowIsActive = IsActive;

        var windowPos = Window.ClientBounds.Location;
        if(windowPos != windowPosition)
            OnWindowMove(windowPos);

        windowPosition = windowPos;

        base.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        float partialTick = coreThread.PartialTick;

        if(takeScreenshot)
        {
            renderingSystem.TakeScreenshot(partialTick);
            takeScreenshot = false;
        }
        else
        {
            renderingSystem.NewFrame(partialTick);
        }

        base.Draw(gameTime);

        frameTime = stopwatch.Elapsed.TotalNanoseconds;
        time = gameTime;
        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;

        startBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    private void OnWindowMove(Point position)
    {
        
    }
}
