using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Simulation;
using Luminix.Workspaces;
using Luminix.IO;
using Luminix.Resources;
using Luminix.Resources.AssetHandlers;
using Luminix.Threading.Bridges;
using Luminix.AssetHandlers;
using System.Collections.Generic;

namespace Luminix.Threading;

public sealed class CoreThread : TickedThread
{
    internal CoreThread(int workerThreadCount) : base(20)
    {   

        this.Name = "CoreThread";
        this.IsBackground = false;
        this.workerThreadCount = workerThreadCount;
    }

    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;

    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);

    private int workerThreadCount;

    private Universe universe;
    private ResourceProvider resources;
    private TypeRegistry typeRegistry;
    private Universe.Controller universeController;
    private Input input;
    private WorkerThreadPool workerThreadPool;
    private AssetIO assetIO;

    protected sealed override void Initialize()
    {
        IsBackground = true;

        Workspace.Create();
        typeRegistry    = new TypeRegistry();
        resources       = new ResourceProvider();
        universe        = new Universe(out universeController);
        input           = new Input();
        workerThreadPool = new WorkerThreadPool(workerThreadCount);
        assetIO          = new AssetIO();

        workerThreadPool.Initialize();

        var assetLoadingHandler = new AssetLoadingHandler(workerThreadPool, assetIO);
        var textureAtlasHandler = new TextureAssetAtlasHandler(workerThreadPool);

        LoadBootAssets(assetLoadingHandler);
        LoadAssets(assetLoadingHandler, textureAtlasHandler);

        Workspace.Current.SetSubState("renderers");
        
        Workspace.Current.SetSubState("types");

        var process = Process.GetCurrentProcess();
        logger.Log("Total threads at initialization: " + process.Threads.Count);
    }

    protected sealed override void OnTick()
    {
        Tick();
        Render();
    }

    protected sealed override void OnDebugTick()
    {
        Tick();
        long updateTime = CurrentElapsedTicks;
        Render();
        long renderTime = CurrentElapsedTicks;
        
        //avg tick time
        TimeSpan elapsedUpdateTime = Stopwatch.GetElapsedTime(0, updateTime);
        TimeSpan elapsedRenderTime = Stopwatch.GetElapsedTime(updateTime, renderTime);

        Interlocked.Exchange(ref tickUpdateTime, elapsedUpdateTime.TotalMicroseconds);
        Interlocked.Exchange(ref tickRenderTime, elapsedRenderTime.TotalMicroseconds);
    }

    private void Tick()
    {
        Bridges.CoreToRender.RenderBridge.Swap();

        input.ReadFrom(Bridges.RenderToCore.InputBridge);
 
        resources.Set(RenderToCore.ResourceBridge.Get());

        universeController.TickRate = TickRate;
        universeController.DeltaTime = UnsafeTickDeltaTime;
        universeController.TickCount = UnsafeTickCount;

        universe.World?.Tick();
    }
    
    private void Render()
    {
        Optimize(TickIntervalInTicks, CurrentElapsedTicks);
    }

    private void Optimize(long maxTime, long time)
    {
        //if(maxTime * 0.6 > time) currWriteStack.Optimize();
    }

    private void LoadBootAssets(AssetLoadingHandler assetLoadingHandler)
    {
        Workspace.Current.SetSubState("bootstrap");
        logger.Log(Workspace.Current.FullCurrentState);

        var loadedAssets = assetLoadingHandler.RunBootLoadingPhase();

        var task = CoreToRender.AssetBridge.RegisterAsync(loadedAssets);

        Workspace.Current.SetSubState("receiving boot assets");
        logger.Log(Workspace.Current.FullCurrentState);
        
        task.Wait();
        resources.Set(RenderToCore.ResourceBridge.Get());
        
        long before = GC.GetTotalMemory(false);
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        long cleared = before - after;
        if(Settings.DebugInitialization)
            logger.Log($"GC cleared: {cleared / 1024.0:F2} KB ({cleared:N0}) bytes)");
    }

    private void LoadAssets(AssetLoadingHandler assetLoadingHandler, TextureAssetAtlasHandler atlasHandler)
    {
        Workspace.Current.SetSubState("loading assets");
        logger.Log(Workspace.Current.FullCurrentState);

        var loadedAssets = assetLoadingHandler.RunLoadingPhase();

        Workspace.Current.SetSubState("generating atlases");
        logger.Log(Workspace.Current.FullCurrentState);

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

        var task = CoreToRender.AssetBridge.RegisterAsync(assets);

        Workspace.Current.SetSubState("receiving assets");
        logger.Log(Workspace.Current.FullCurrentState);
        
        task.Wait();
        resources.Set(RenderToCore.ResourceBridge.Get());

        long before = GC.GetTotalMemory(false);
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        long cleared = before - after;
        if(Settings.DebugInitialization)
            logger.Log($"GC cleared: {cleared / 1024.0:F2} KB ({cleared:N0} bytes)");
    }
}
