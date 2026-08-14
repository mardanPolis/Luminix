using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Luminix.IO;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using Game.Simulation;
using Luminix.Rendering;
using Luminix.UI;
using Luminix.RegistrySystem;

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
    private Assets assets;
    private Resources resources;
    private RegistryManager registryManager;
    private WorkerThreadPool workerThreadPool;
    
    protected sealed override void Initialize()
    {
        IsBackground = true;

        logger.Log($"Started initialization");

        universe           = new Universe(out universeController);
        graphics           = new Graphics(renderBridge, out graphicsController);
        input              = new Input(inputBridge, out inputController);
        registryManager    = new RegistryManager();
        workerThreadPool   = new WorkerThreadPool(workerThreadCount);
        assets             = new Assets(registryManager, workerThreadPool, assetBridge);
        resources          = new Resources();

        //TODO : Add entityType registration.

        registryManager.Freeze();

        var stopwatch = Stopwatch.StartNew();

        var bootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: true);
        assets.HandleAssets(bootAssetInfos);

        assets.FinalizeAssets(false);

        var nonBootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: false);
        assets.HandleAssets(nonBootAssetInfos);

        assets.FinalizeAssets(true);

        long before = GC.GetTotalMemory(false);
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        long cleared = before - after;
        logger.Log($"GC cleared: {cleared / 1024.0:F2} KB ({cleared:N0} bytes)");
        logger.Log($"Loaded assets in {stopwatch.Elapsed.Milliseconds}ms");

        logger.Log("Total threads at initialization: " + Process.GetCurrentProcess().Threads.Count);

        //TODO : debug actions

        // debugKeyActions[0] = () => Settings.DebugInfo = !Settings.DebugInfo;                 //F1
        // debugKeyActions[1] = () => Settings.DebugConsole = !Settings.DebugConsole;           //F2
        // debugKeyActions[2] = () => Settings.DebugInspector = !Settings.DebugInspector;       //F3
        // debugKeyActions[3] = () => Settings.DebugGizmosPanel = !Settings.DebugGizmosPanel;   //F4
        // debugKeyActions[4] = () => Settings.DebugEventPanel = !Settings.DebugEventPanel;     //F5
        // debugKeyActions[5] = () => { logger.Wrn("Fullscreen not implemented!"); };           //F11
        // debugKeyActions[6] = () => { logger.Wrn("Screenshot not implemented!"); };           //F12

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

        inputController.UpdateData();

        resources.Set(resourceBridge.Get());

        universeController.TickRate = TickRate;
        universeController.TickTime = TickTime;
        universeController.TickCount = TickCount;
        universeController.TickDeltaTime = TickDeltaTime;
        universeController.TickAllocated = TickAllocated;

        universeController.Tick();

        // var mousePosition = input.MousePosition;
        // inputController.IsMouseOverUI = uiManagerController.isMouseColliding((int)mousePosition.X, (int)mousePosition.Y);
    }
    
    private void Render()
    {
        graphicsController.BeginRender();

        // entity rendering

        graphicsController.StopRender();
    }
}
