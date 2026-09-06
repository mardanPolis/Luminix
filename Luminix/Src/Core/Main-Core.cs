using System;
using System.Diagnostics;
using Luminix.Collections;
using Luminix.IO;
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

        try
        {
            renderBridge = ThreadBridges.GetBridge<RenderBridge>("RenderThread");
            inputBridge = ThreadBridges.GetBridge<InputBridge>("RenderThread");
            resourceBridge = ThreadBridges.GetBridge<ResourceBridge>("RenderThread");
            assetBridge = ThreadBridges.GetBridge<AssetBridge>("RenderThread");
            
            coreThread = (TickedThread)Thread.CurrentThread;

            Thread.CurrentThread.IsBackground = true;

            TypeRegistry.Add(typeof(EntityType));
            TypeRegistry.Add(typeof(EntityType), "EntityType");

            TypeRegistry.SetDefaultSubType(typeof(IComponentType), typeof(ComponentType<>), true);

            logger.NewLog(Logger.Log.Level.Info, $"Build-Vers: {LuminixManifest.BuildVers}");
            logger.NewLog(Logger.Log.Level.Info, $"Build-Date: {LuminixManifest.BuildDate}");

            logger.NewLog(Logger.Log.Level.Info, $"Started initialization");

            universe           = new Universe(out universeController);
            graphics           = new Graphics(renderBridge, out graphicsController);
            input              = new Input(inputBridge, out inputController);
            defs               = new Defs();
            assets             = new Assets(assetBridge);
            resources          = new Resources();

            defs.NewDefType<EntityType>(); // built in

            defs.FinalizeTypes();

            var bootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: true);
            foreach(var info in bootAssetInfos)
                assets.HandleAsset(info);

            assets.FinalizeAssets(false);

            logger.NewLog(Logger.Log.Level.Info, "Loaded boot assets!");

            var nonBootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: false);
            foreach(var info in nonBootAssetInfos)
                assets.HandleAsset(info);

            assets.FinalizeAssets(true);

            defs.PatchNodes();
            defs.InheritNodes();
            defs.DeserializeNodes();

            defs.FinalizeDefs();

            Thread.CurrentThread.IsBackground = false;
        }
        catch(Exception ex)
        {
            logger.NewLog(Logger.Log.Level.Error, $"Fatal Initialization Error: {ex}");
            logger.Flush();
            throw;
        }
    }

    internal void Update()
    {
        try
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
        catch(Exception ex)
        {
            logger.NewLog(Logger.Log.Level.Error, $"Fatal Update Error: {ex}");
            logger.Flush();
            throw;
        }
    }
    
    internal void Render()
    {
        try
        {
            graphicsController.BeginRender();

            // entity rendering

            graphicsController.StopRender();
        }
        catch(Exception ex)
        {
            logger.NewLog(Logger.Log.Level.Error, $"Fatal Render Error: {ex}");
            logger.Flush();
            throw;
        }
    }

    internal void OnStop()
    {
        
    }
}