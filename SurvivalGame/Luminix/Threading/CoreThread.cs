using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Simulation;
using Luminix.Rendering;
using Luminix.Workspaces;

namespace Luminix.Threading;

public sealed class CoreThread : WorkerThread
{
    internal CoreThread() : base(20)
    {   

        this.Name = "CoreThread";
        this.IsBackground = false;
    }

    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;

    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);

    private Universe universe;
    private Universe.Controller universeController;
    private Input input;

    protected sealed override void Initialize()
    {
        Workspace.Create();
        universe = new Universe(out universeController);
        input = new Input();
    }

    protected sealed override void OnTick()
    {
        Bridges.CoreToRender.Swap();

        input.ReadFrom(Bridges.RenderToCore.InputBridge);

        GameTick();
        Render();
    }

    protected sealed override void OnDebugTick()
    {
        Bridges.CoreToRender.Swap();

        input.ReadFrom(Bridges.RenderToCore.InputBridge);

        GameTick();
        long updateTime = CurrentElapsedTicks;
        Render();
        long renderTime = CurrentElapsedTicks;
        
        //avg tick time
        TimeSpan elapsedUpdateTime = Stopwatch.GetElapsedTime(0, updateTime);
        TimeSpan elapsedRenderTime = Stopwatch.GetElapsedTime(updateTime, renderTime);

        Interlocked.Exchange(ref tickUpdateTime, elapsedUpdateTime.TotalMicroseconds);
        Interlocked.Exchange(ref tickRenderTime, elapsedRenderTime.TotalMicroseconds);
    }

    private void GameTick()
    {
        universeController.TickRate = TickRate;
        universeController.DeltaTime = UnsafeTickDeltaTime;
        universeController.TickCount = UnsafeTickCount;

        universe.World.Tick();
    }
    
    private void Render()
    {
        Optimize(TickIntervalInTicks, CurrentElapsedTicks);
    }

    private void Optimize(long maxTime, long time)
    {
        //if(maxTime * 0.6 > time) currWriteStack.Optimize();
    }
}
