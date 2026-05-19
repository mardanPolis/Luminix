using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Simulation;
using Luminix.Workspaces;
using Luminix.IO;
using Luminix.Resources;
using Luminix.Resources.AssetHandlers;
using Luminix.Threading.Bridges;

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
        Workspace.Create();
        typeRegistry = new TypeRegistry();
        resources    = new ResourceProvider();
        universe     = new Universe(out universeController);
        input        = new Input();
        workerThreadPool = new WorkerThreadPool(workerThreadCount);
        assetIO          = new AssetIO();

        Workspace.Current.SetSubState("Loading BootAssets");
        var assetLoader = new AssetLoadingHandler(workerThreadPool, assetIO, CoreToRender.AssetBridge);
        assetLoader.RunBootLoadingPhase();
        resources.WaitForResources(assetLoader.GetLoadedBootAssets(), RenderToCore.ResourceBridge);

        //Initialize Boot Renderers

        Workspace.Current.SetSubState("Loading Assets");
        assetLoader.RunLoadingPhase((string assetPath, int loaded, int toLoad) =>
        {
            BootRender(assetPath, loaded, toLoad);
        }); //Lambda - OnLoad

        Workspace.Current.SetSubState("Receiving Assets");
        resources.WaitForResources(assetLoader.GetLoadedAssets(), RenderToCore.ResourceBridge, (string asset, int received, int toReceive) =>
        {
            BootRender(asset, received, toReceive);
        }); //Lambda - OnReceive

        Workspace.Current.SetSubState("Initializing Renderers");
        //Renderers

        Workspace.Current.SetSubState("Creating Types");
        //Types

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

        universeController.TickRate = TickRate;
        universeController.DeltaTime = UnsafeTickDeltaTime;
        universeController.TickCount = UnsafeTickCount;

        universe.World.Tick();
    }
    
    private void Render()
    {
        Optimize(TickIntervalInTicks, CurrentElapsedTicks);
    }

    private void BootRender(string info, int current, int max)
    {
        
    }

    private void Optimize(long maxTime, long time)
    {
        //if(maxTime * 0.6 > time) currWriteStack.Optimize();
    }
}
