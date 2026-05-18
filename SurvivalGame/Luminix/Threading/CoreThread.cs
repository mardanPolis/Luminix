using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Simulation;
using Luminix.Rendering;
using Luminix.Workspaces;
using SurvivalGame.Luminix.IO;

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
    private Resources resources;
    private TypeRegistry typeRegistry;
    private Universe.Controller universeController;
    private Input input;

    protected sealed override void Initialize()
    {
        Workspace.Create();
        typeRegistry = new TypeRegistry();
        resources = new Resources();
        universe  = new Universe(out universeController);
        input     = new Input();

        //TO DO: add mod loading.

        AssetIO assetIO = new AssetIO();
        //multithreaded
        //first load all boot assets multithreaded, give them all to the render thread, wait, receive resource handles.
        //then load the rest of the assets, giving them to the render thread. multithreaded. BootRender should only called once a asset stream is received from worker thread or created in this thread.
        //load boot assets, no rendering

        //load assets
        foreach (var entry in assetIO.LoadAssets())
        {

            BootRender();
        }
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

    private void BootRender()
    {
        
    }

    private void Optimize(long maxTime, long time)
    {
        //if(maxTime * 0.6 > time) currWriteStack.Optimize();
    }
}
