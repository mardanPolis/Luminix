using System;
using System.Diagnostics;
using Luminix.Threading;
using Luminix.Threading.Bridges;

namespace Luminix;

internal sealed class MainCore
{
    internal MainCore() {}

    private TickedThread coreThread;

    private RenderBridge renderBridge;
    private InputBridge inputBridge;
    private ResourceBridge resourceBridge;
    private AssetBridge assetBridge;

    private Logger logger;

    private Universe universe;
    private Universe.Controller universeController;
    private Graphics graphics;
    private Graphics.Controller graphicsController;
    private Input input;
    private Input.Controller inputController;
    private Defs defs;
    private Assets assets;
    private Resources resources;

    internal void Initialize()
    {
        logger = Logger.Get();
        logger.EchoToConsole = true;

        logger.NewLog(Logger.Log.Level.Info, $"Thread: {Thread.CurrentThread}");

        renderBridge = ThreadBridges.GetBridge<RenderBridge>("RenderThread");
        inputBridge = ThreadBridges.GetBridge<InputBridge>("RenderThread");
        resourceBridge = ThreadBridges.GetBridge<ResourceBridge>("RenderThread");
        assetBridge = ThreadBridges.GetBridge<AssetBridge>("RenderThread");
        
        coreThread = (TickedThread)Thread.CurrentThread;

        Thread.CurrentThread.IsBackground = true;

        logger.NewLog(Logger.Log.Level.Info, $"Started initialization");

        universe           = new Universe(out universeController);
        graphics           = new Graphics(renderBridge, out graphicsController);
        input              = new Input(inputBridge, out inputController);
        defs               = new Defs();
        assets             = new Assets(assetBridge);
        resources          = new Resources();

        var stopwatch = Stopwatch.StartNew();

        logger.NewLog(Logger.Log.Level.Info, $"Started Initialization.");
        logger.NewLog(Logger.Log.Level.Info, $"Build-Vers: {LuminixManifest.BuildVers}");
        logger.NewLog(Logger.Log.Level.Info, $"Build-Date: {LuminixManifest.BuildDate}");

        logger.NewLog(Logger.Log.Level.Info, "Namespaces:");
        foreach (var ns in LuminixManifest.Namespaces)
            logger.NewLog(Logger.Log.Level.Info, ns);

        defs.NewDefType<EntityType>(); // built in

        defs.FinalizeTypes();

        var bootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: true);
        foreach(var info in bootAssetInfos)
            assets.HandleAsset(info);

        assets.FinalizeAssets(false);

        var nonBootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: false);
        foreach(var info in nonBootAssetInfos)
            assets.HandleAsset(info);

        assets.FinalizeAssets(true);

        defs.PatchNodes();
        defs.InheritNodes();
        defs.DeserializeNodes();

        defs.FinalizeDefs();

        long before = GC.GetTotalMemory(false);
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        long cleared = before - after;
        logger.NewLog(Logger.Log.Level.Info, $"GC cleared: {cleared / 1024.0:F2} KB ({cleared:N0} bytes)");
        logger.NewLog(Logger.Log.Level.Info, $"Initialized in {stopwatch.Elapsed.Milliseconds}ms");


        Thread.CurrentThread.IsBackground = false;
    }

    internal void Update()
    {
        logger.Refresh();

        graphicsController.SwapBuffers();

        inputController.UpdateData();

        resources.Set(resourceBridge.Get());

        uint tickRate = coreThread.TickRate;

        universeController.TickRate = tickRate;
        universeController.TickTime = coreThread.TickTime;
        universeController.TickCount = coreThread.TickCount;
        universeController.TickDeltaTime = coreThread.TickDeltaTime;
        universeController.TickAllocated = coreThread.TickAllocated;

        // var mousePosition = input.MousePosition;
        // inputController.IsMouseOverUI = uiManagerController.isMouseColliding((int)mousePosition.X, (int)mousePosition.Y);


        if(tickRate != universeController.TickRate)
            coreThread.TickRate = tickRate;

    }
    
    internal void Render()
    {
        graphicsController.BeginRender();

        // entity rendering

        graphicsController.StopRender();
    }
}