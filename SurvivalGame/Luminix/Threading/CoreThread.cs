using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Luminix.IO;
using Luminix.Resources;
using Luminix.Resources.AssetHandlers;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using Game.Simulation;
using Luminix.Rendering;

namespace Luminix.Threading;

public sealed class CoreThread : TickedThread
{
    internal CoreThread(RenderBridge renderBridge, AssetBridge assetBridge, InputBridge inputBridge, ResourceBridge resourceBridge, int workerThreadCount) : base(60)
    {   
        this.Name = "CoreThread";
        this.IsBackground = false;
        this.workerThreadCount = workerThreadCount;

        this.renderBridge = renderBridge;
        this.inputBridge = inputBridge;
        this.resourceBridge = resourceBridge;
        this.assetBridge = assetBridge;
    }

    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;

    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);

    private int workerThreadCount;

    private RenderBridge renderBridge;
    private InputBridge inputBridge;
    private ResourceBridge resourceBridge;
    private AssetBridge assetBridge;

    private Universe universe;
    private Universe.Controller universeController;
    private Graphics graphics;
    private Graphics.Controller graphicsController;
    private ResourceProvider resources;
    private EntityTypeRegistry entityTypeRegistry;
    private Input input;
    private WorkerThreadPool workerThreadPool;
    private AssetIO assetIO;

    private Keys[] debugKeys = { Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F11, Keys.F12 };
    private Action[] debugKeyActions = new Action[7];
    
    private TextureResource circle;

    private SystemDebugRenderer systemDebugRenderer;

    protected sealed override void Initialize()
    {
        IsBackground = true;

        logger.Log($"Started initialization");

        Workspace.Create().SetSubState("bootstrap");
        universe           = new Universe(out universeController);
        graphics           = new Graphics(renderBridge, out graphicsController);
        resources          = new ResourceProvider();
        entityTypeRegistry = new EntityTypeRegistry();
        input              = new Input();
        workerThreadPool = new WorkerThreadPool(workerThreadCount);
        assetIO          = new AssetIO();

        workerThreadPool.Initialize();

        var assetLoadingHandler = new AssetLoadingHandler(workerThreadPool, assetIO);
        var textureAtlasHandler = new TextureAssetAtlasHandler(workerThreadPool);

        var stopwatch = Stopwatch.StartNew();

        //TODO : Add entityType registration.

        entityTypeRegistry.FinalizeRegistry();

        stopwatch.Restart();
        LoadBootAssets(assetLoadingHandler);
        LoadAssets(assetLoadingHandler, textureAtlasHandler);
        logger.Log($"{Name} loaded assets in {stopwatch.Elapsed.Milliseconds}ms");

        Workspace.Current.SetSubState("types");

        var process = Process.GetCurrentProcess();
        logger.Log("Total threads at initialization: " + process.Threads.Count);

        debugKeyActions[0] = () => Settings.DebugInfo = !Settings.DebugInfo;                 //F1
        debugKeyActions[1] = () => Settings.DebugConsole = !Settings.DebugConsole;           //F2
        debugKeyActions[2] = () => Settings.DebugInspector = !Settings.DebugInspector;       //F3
        debugKeyActions[3] = () => Settings.DebugGizmosPanel = !Settings.DebugGizmosPanel;   //F4
        debugKeyActions[4] = () => Settings.DebugEventPanel = !Settings.DebugEventPanel;     //F5
        debugKeyActions[5] = () => { logger.Wrn("Fullscreen not implemented!"); };           //F11
        debugKeyActions[6] = () => { logger.Wrn("Screenshot not implemented!"); };           //F12

        IsBackground = false;

        //TODO : Remove
        circle = resources.GetResource<TextureResource>("circle");

        systemDebugRenderer = new SystemDebugRenderer();

        universe.LoadWorld("SimWorld");
        Camera camera = new Camera();
        _ = new CameraMover(camera);
    }

    protected override void DebugInitialize()
    {

    }

    protected override void Tick()
    {
        Update();
        Render();
    }

    private void Update()
    {
        graphicsController.SwapBuffers();

        input.ReadFrom(inputBridge.ReadBuffer);
        inputBridge.Swap();

        resources.Set(resourceBridge.Get());

        for(int key = 0; key < debugKeys.Length; key++)
        {
            if(input.GetKeyUp(debugKeys[key]))
                debugKeyActions[key]();
        }

        universeController.TickRate = TickRate;
        universeController.TickTime = TickTime;
        universeController.TickCount = TickCount;
        universeController.TickDeltaTime = TickDeltaTime;
        universeController.TickAllocated = TickAllocated;

        universe.World?.Tick();
    }
    
    private void Render()
    {
        graphicsController.BeginRender();
        systemDebugRenderer.Render();
        graphicsController.StopRender();
    }

    private void LoadBootAssets(AssetLoadingHandler assetLoadingHandler)
    {
        var loadedAssets = assetLoadingHandler.Run(true);

        var task = assetBridge.RegisterAsync(loadedAssets);

        Workspace.Current.SetSubState("receiving boot assets");
        
        task.Wait();
        resources.Set(resourceBridge.Get());
    }

    private void LoadAssets(AssetLoadingHandler assetLoadingHandler, TextureAssetAtlasHandler atlasHandler)
    {
        Workspace.Current.SetSubState("loading assets");

        var loadedAssets = assetLoadingHandler.Run(false);

        Workspace.Current.SetSubState("generating atlases");

        List<Asset> assets = [];
        List<TextureAsset> textureAssets = [];

        foreach (var asset in loadedAssets)
        {
            if(asset is TextureAsset textureAsset)
                textureAssets.Add(textureAsset);
            else
                assets.Add(asset);
        }

        foreach (AtlasAsset atlas in atlasHandler.Run(textureAssets))
        {
            assets.Add(atlas);
            
            if(Settings.DebugAssets && Settings.DebugInitialization)
                assetIO.SaveTexture(atlas.Name, atlas.Colors, atlas.Size);
        }

        if(Settings.DebugInitialization && Settings.DebugAssets)
        {
            logger.Log($"Loaded Asset count: {assets.Count}");
            foreach(var asset in assets)
            {
                logger.Log($" - asset: {asset}");
                if(asset is AtlasAsset atlasAsset)
                {
                    foreach(var texture in atlasAsset.Regions)
                    {
                        logger.Log($" - - asset: {texture}");
                    }
                }
            }
        }
        
        var task = assetBridge.RegisterAsync(assets);

        Workspace.Current.SetSubState("receiving assets");
        
        task.Wait();
        resources.Set(resourceBridge.Get());

        long before = GC.GetTotalMemory(false);
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        long cleared = before - after;
        if(Settings.DebugInitialization)
            logger.Log($"GC cleared: {cleared / 1024.0:F2} KB ({cleared:N0} bytes)");
    }
}
