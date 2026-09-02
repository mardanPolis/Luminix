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
using Luminix.InputSystem.Handlers;
using Luminix.Threading.Buffers;
using Luminix.IO;

namespace Luminix;
internal sealed class MainRender : Microsoft.Xna.Framework.Game
{
    internal MainRender(TickedThread coreThread)
    {
        logger = Logger.Get();
        logger.EchoToConsole = true;

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

        this.mainThread = Thread.CurrentThread;
        this.coreThread = coreThread;

        logger.NewLog(Logger.Log.Level.Info, $"RenderThread bootstrap in {stopwatch.Elapsed.Milliseconds}ms");
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

    protected override void Initialize()
    {
        try
        {
            logger.NewLog(Logger.Log.Level.Info, $"Started initialization in {stopwatch.Elapsed.Milliseconds}ms");

            renderBridge = ThreadBridges.GetBridge<RenderBridge>("CoreThread");
            coreInputBridge = ThreadBridges.GetBridge<InputBridge>("CoreThread");
            coreResourceBridge = ThreadBridges.GetBridge<ResourceBridge>("CoreThread");
            coreAssetBridge = ThreadBridges.GetBridge<AssetBridge>("CoreThread");
            
            startBytes = GC.GetAllocatedBytesForCurrentThread();
            
            AssetIO assetIO = new AssetIO();

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

            resourceManager.CreateResource(assetIO.LoadXnbAsset(LuminixManifest.Assembly, "Assets.Built-In.Fonts.IBMPlexMono-Bold.xnb"));
            resourceManager.CreateResource(assetIO.LoadXnbAsset(LuminixManifest.Assembly, "Assets.Built-In.Fonts.IBMPlexMono-Medium.xnb"));
            resourceManager.CreateResource(assetIO.LoadXnbAsset(LuminixManifest.Assembly, "Assets.Built-In.Fonts.IBMPlexMono-Regular.xnb"));
            resourceManager.CreateResource(assetIO.LoadXnbAsset(LuminixManifest.Assembly, "Assets.Built-In.Shaders.GenericShader.xnb"));

            resourceManager.Resources.Set(resourceManager.TextureResources, resourceManager.AudioResources, resourceManager.FontResources, resourceManager.ShaderResources);

            //Adapters

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

        logger.NewLog(Logger.Log.Level.Info, $"RenderThread allocated {allocated / 1024.0:F2} KB | ({allocated:N0} bytes) during initialization!");
    }

    protected override void Update(GameTime gameTime)
    {
        stopwatch.Restart();
        renderInfoBuffer = renderBridge.CurrentWriteInfoBuffer ?? renderBridge.CreateInfoBuffer();

        inputHandler.Update(IsActive);

        /*| Resources */
        if(coreAssetBridge.AssetsPassing)
        {
            coreAssetBridge.ConsumeAssets(resourceManager.CreateResource, out List<TaskCompletionSource> tsks);
            resourceManager.Resources.Set(resourceManager.TextureResources, resourceManager.AudioResources, resourceManager.FontResources, resourceManager.ShaderResources);
            coreResourceBridge.Set(resourceManager.Resources.GetResources());

            for(int i = 0; i < tsks.Count; i++)
                tsks[i].SetResult();
                
        }
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

        renderInfoBuffer.Updated = true;
        
        allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        startBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    private void OnWindowMove(Point position)
    {
        
    }
}
