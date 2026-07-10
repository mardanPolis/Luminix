using System;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Luminix.Rendering;
using Luminix.Resources;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Core.InputHandlers;
using Luminix.Resources.AssetHandlers;
using Luminix.IO;
using Luminix.Resources.BuiltIn;
using Luminix.Threading.Buffers;
using Microsoft.Xna.Framework.Graphics;

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
        workspace = new Workspace(out workspaceController);
        embeddedIO = new EmbeddedIO();

        logger = Logger.Get();
        logger.Log($"Started initialization");
        logger.Log($"Build-Vers: {version}");
        logger.Log($"Build-Date: {buildDate}");

        stopwatch = Stopwatch.StartNew();

        logger.Log($"Creating GraphicsDeviceManager");
        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = true;
        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
        graphics.IsFullScreen = false;
        graphics.HardwareModeSwitch = false;
        graphics.PreferHalfPixelOffset = false;

        resourceManager = new ResourceManager(this);
        IsMouseVisible = true;

        logger.Log($"Initializing threads");
        //initialize threads
        {
            renderBridge = new RenderBridge();
            coreInputBridge = new InputBridge();
            coreResourceBridge = new ResourceBridge();
            coreAssetBridge = new AssetBridge();

            coreThread = new CoreThread(renderBridge, coreAssetBridge, coreInputBridge, coreResourceBridge);

        }
        //starting threads
        coreThread.Start();
    }

    //Core Bridges
    private RenderBridge renderBridge;
    private InputBridge coreInputBridge;
    private ResourceBridge coreResourceBridge;
    private AssetBridge coreAssetBridge;

    //RenderThread
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
    private Workspace.Controller workspaceController;
    private EmbeddedIO embeddedIO;
    private RenderingSystem renderingSystem;

    private ResourceManager resourceManager;
    private GraphicsDeviceManager graphics;

    private InputHandler inputHandler;
    private AssetRegistrationHandler assetRegistrationHandler;

    private RenderInfoBuffer renderInfoBuffer;

    private bool takeScreenshot = false;

    public Thread MainSystemThread => mainThread;
    public CoreThread CoreThread => coreThread;

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

    protected override void Initialize()
    {
        try
        {
            logger.Log($"Initializing");
            startBytes = GC.GetAllocatedBytesForCurrentThread();
            
            ThreadPool.SetMaxThreads(3, 1);

            //systems
            renderingSystem = new RenderingSystem(this, renderBridge);

            //handlers
            inputHandler = new InputHandler(coreInputBridge);
            assetRegistrationHandler = new AssetRegistrationHandler(coreAssetBridge, coreResourceBridge, resourceManager);
            
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
                    resourceManager.CreateResource(asset);
                }
                catch(Exception ex)
                {
                    logger.Err($"Failed to load embedded asset '{asset.Name}', reason: {ex}");
                }
            }

            foreach(var adapter in GraphicsAdapter.Adapters)
            {
                logger.Log($"Adapter: {adapter.DisplayName}");
                foreach(var mode in adapter.SupportedDisplayModes)
                {
                    logger.Log($" - Mode: {mode.ToString()}");
                }
            }

            //Renderer initialization   
            renderingSystem.Initialize();

            TargetElapsedTime = TimeSpan.FromSeconds(1d / 144d);
            IsFixedTimeStep = true;

            windowPosition = Window.ClientBounds.Location;
        }
        catch
        {
            Logger.Get().Err($"Exception happened during initialization");
            throw;
        }

        workspaceController.Start();

        logger.Log($"RenderThread initialized in {stopwatch.Elapsed.Milliseconds}ms");

        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;

        logger.Log($"RenderThread allocated {allocated / 1024.0:F2} KB ({allocated:N0}) bytes during initialization!");
    }

    protected override void Update(GameTime gameTime)
    {
        stopwatch.Restart();
        renderInfoBuffer = renderBridge.CurrentWriteInfoBuffer ?? renderBridge.CreateInfoBuffer();

        inputHandler.Update(IsActive);
        assetRegistrationHandler.Run();

        //TODO : add render settings and so on able to be access from other threads
    
        var windowPos = Window.ClientBounds.Location;
        if(windowPos != windowPosition)
            OnWindowMove(windowPos);

        windowPosition = windowPos;

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
        // TODO : renderInfoBuffer.FrameRate;
        renderInfoBuffer.FrameDeltaTime += time.ElapsedGameTime.TotalMilliseconds;
        renderInfoBuffer.FrameTime += frameTime;

        renderInfoBuffer.updated = true;
        
        allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        startBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    private void OnWindowMove(Point position)
    {
        
    }
}
