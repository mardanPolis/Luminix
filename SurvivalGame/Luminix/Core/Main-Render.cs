using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Rendering;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Core.Handlers;
using Luminix.Threading.Buffers;

namespace Luminix;
internal sealed class MainRender : Microsoft.Xna.Framework.Game
{
    internal MainRender(TickedThread coreThread)
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

        this.coreThread = coreThread;
    }

    //Core Bridges
    private RenderBridge renderBridge;
    private InputBridge coreInputBridge;
    private ResourceBridge coreResourceBridge;
    private AssetBridge coreAssetBridge;

    //RenderThread
    private readonly Thread mainThread;
    private readonly TickedThread coreThread = null;

    private long startBytes = 0;
    private long allocated = 0;
    private double frameTime;

    private Point windowPosition;

    private Stopwatch stopwatch;
    private GameTime time;
    private Logger logger;

    private Workspace workspace;
    private Workspace.Controller workspaceController;
    private RenderingSystem renderingSystem;

    private ResourceManager resourceManager;
    private GraphicsDeviceManager graphics;

    private InputHandler inputHandler;

    private RenderInfoBuffer renderInfoBuffer;

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

    internal void Start()
    {
        
    }

    protected override void Initialize()
    {
        try
        {
            logger = Logger.Get();
            logger.EchoToConsole = true;
            logger.NewLog(Logger.Log.Level.Info, $"Started initialization");

            renderBridge = ThreadBridges.GetBridge<RenderBridge>("CoreThread");
            coreInputBridge = ThreadBridges.GetBridge<InputBridge>("CoreThread");
            coreResourceBridge = ThreadBridges.GetBridge<ResourceBridge>("CoreThread");
            coreAssetBridge = ThreadBridges.GetBridge<AssetBridge>("CoreThread");
            
            logger.NewLog(Logger.Log.Level.Info, $"Initializing");
            startBytes = GC.GetAllocatedBytesForCurrentThread();
            
            System.Threading.ThreadPool.SetMaxThreads(3, 1);

            //systems
            renderingSystem = new RenderingSystem(this, renderBridge);

            //handlers
            inputHandler = new InputHandler(coreInputBridge);
            
            //base initialization
            base.Initialize();

            TextureAsset nullTextureAsset = new ("NullTexture", Resources.BuiltIn.NullTextureColor, new Vector2(32, 32));
            TextureAsset pixelTextureAsset = new ("Pixel", Resources.BuiltIn.PixelTextureColor, new Vector2(1, 1));
            resourceManager.CreateResource(nullTextureAsset);
            resourceManager.CreateResource(pixelTextureAsset);

            //Loading Embedded assets

            foreach(var adapter in GraphicsAdapter.Adapters)
            {
                logger.NewLog(Logger.Log.Level.Info, $"Adapter: {adapter.DisplayName}");
                foreach(var mode in adapter.SupportedDisplayModes)
                {
                    logger.NewLog(Logger.Log.Level.Info, $" - Mode: {mode.ToString()}");
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
            Logger.Get().NewLog(Logger.Log.Level.Error, $"Exception happened during initialization");
            throw;
        }

        workspaceController.Start();

        logger.NewLog(Logger.Log.Level.Info, $"RenderThread initialized in {stopwatch.Elapsed.Milliseconds}ms");

        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;

        logger.NewLog(Logger.Log.Level.Info, $"RenderThread allocated {allocated / 1024.0:F2} KB ({allocated:N0}) bytes during initialization!");
    }

    protected override void Update(GameTime gameTime)
    {
        stopwatch.Restart();
        renderInfoBuffer = renderBridge.CurrentWriteInfoBuffer ?? renderBridge.CreateInfoBuffer();

        inputHandler.Update(IsActive);

        /*| Resources */
        coreAssetBridge.ConsumeAssets(resourceManager.CreateResource, out List<TaskCompletionSource> tsks);
        resourceManager.Resources.Set(resourceManager.TextureResources, resourceManager.AudioResources, resourceManager.FontResources, resourceManager.ShaderResources);
        coreResourceBridge.Set(resourceManager.Resources.GetResources());

        for(int i = 0; i < tsks.Count; i++)
            tsks[i].SetResult();
            
        /* Resources END |*/

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
