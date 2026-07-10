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
using Luminix.UI;

namespace Luminix.Threading;

public sealed class CoreThread : TickedThread
{
    internal CoreThread(RenderBridge renderBridge, AssetBridge assetBridge, InputBridge inputBridge, ResourceBridge resourceBridge) : base(60)
    {   
        this.Name = "CoreThread";
        this.IsBackground = false;
        this.workerThreadCount = Environment.ProcessorCount - 2; //FIXME : workerThread count

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
    private Input input;
    private Input.Controller inputController;
    private UIManager uiManager;
    private UIManager.Controller uiManagerController;
    private ResourceProvider resources;
    private EntityTypeRegistry entityTypeRegistry;
    private WorkerThreadPool workerThreadPool;
    private AssetIO assetIO;

    private Keys[] debugKeys = { Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F11, Keys.F12 };
    private Action[] debugKeyActions = new Action[7];
    
    protected sealed override void Initialize()
    {
        IsBackground = true;

        logger.Log($"Started initialization");

        universe           = new Universe(out universeController);
        graphics           = new Graphics(renderBridge, out graphicsController);
        input              = new Input(inputBridge, out inputController);
        uiManager          = new UIManager(graphics, out uiManagerController);
        resources          = new ResourceProvider();
        entityTypeRegistry = new EntityTypeRegistry();
        workerThreadPool = new WorkerThreadPool(workerThreadCount);
        assetIO          = new AssetIO();

        //TODO : Add entityType registration.

        entityTypeRegistry.FinalizeRegistry();

        LoadAssets();

        logger.Log("Total threads at initialization: " + Process.GetCurrentProcess().Threads.Count);

        //TODO : debug actions

        // debugKeyActions[0] = () => Settings.DebugInfo = !Settings.DebugInfo;                 //F1
        // debugKeyActions[1] = () => Settings.DebugConsole = !Settings.DebugConsole;           //F2
        // debugKeyActions[2] = () => Settings.DebugInspector = !Settings.DebugInspector;       //F3
        // debugKeyActions[3] = () => Settings.DebugGizmosPanel = !Settings.DebugGizmosPanel;   //F4
        // debugKeyActions[4] = () => Settings.DebugEventPanel = !Settings.DebugEventPanel;     //F5
        debugKeyActions[5] = () => { logger.Wrn("Fullscreen not implemented!"); };           //F11
        debugKeyActions[6] = () => { logger.Wrn("Screenshot not implemented!"); };           //F12

        IsBackground = false;
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
        uiManagerController.Clear();

        inputController.UpdateData();

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

        universeController.Tick();

        var mousePosition = input.MousePosition;
        inputController.IsMouseOverUI = uiManagerController.isMouseColliding((int)mousePosition.X, (int)mousePosition.Y);
    }
    
    private void Render()
    {
        graphicsController.BeginRender();
        uiManagerController.Render();
        graphicsController.StopRender();
    }

    private void LoadAssets()
    {
        var stopwatch = Stopwatch.StartNew();

        var assetLoadingHandler = new AssetLoadingHandler(workerThreadPool, assetIO);
        var textureAtlasHandler = new TextureAssetAtlasHandler(workerThreadPool);

        //Boot
        {
            var loadedAssets = assetLoadingHandler.Run(true);

            var task = assetBridge.RegisterAsync(loadedAssets);

            task.Wait();
            resources.Set(resourceBridge.Get());
        }

        //Non-Boot
        {
            var loadedAssets = assetLoadingHandler.Run(false);

            List<Asset> assets = [];
            List<TextureAsset> textureAssets = [];

            foreach (var asset in loadedAssets)
            {
                if(asset is TextureAsset textureAsset)
                    textureAssets.Add(textureAsset);
                else
                    assets.Add(asset);
            }

            foreach (AtlasAsset atlas in textureAtlasHandler.Run(textureAssets))
            {
                assets.Add(atlas);
                
                // if(Settings.DebugAssets && Settings.DebugInitialization)
                //     assetIO.SaveTexture(atlas.Name, atlas.Colors, atlas.Size);
            }

            //TODO : Debug
            if(true)
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

            task.Wait();
            resources.Set(resourceBridge.Get());

            long before = GC.GetTotalMemory(false);
            GC.Collect();
            long after = GC.GetTotalMemory(true);

            long cleared = before - after;
            logger.Log($"GC cleared: {cleared / 1024.0:F2} KB ({cleared:N0} bytes)");
        }

        logger.Log($"Loaded assets in {stopwatch.Elapsed.Milliseconds}ms");
    }
}
