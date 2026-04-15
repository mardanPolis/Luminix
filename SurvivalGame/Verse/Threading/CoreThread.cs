using System;
using System.Diagnostics;
using System.Threading;
using Verse.Simulation;
using Verse.Rendering;

namespace Verse.Threading;

public sealed class CoreThread : WorkerThread
{
    private CoreThread() : base(20)
    {   

        this.Name = "CoreThread";
        this.IsBackground = false;
    }

    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;

    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);

    private Input input;


    protected sealed override void OnTick()
    {
        Bridges.CoreToRender.Swap();

        input.ReadFrom(Bridges.RenderToCore.InputBridge);

        WorldTick();
        Render();
    }

    protected sealed override void OnDebugTick()
    {
        Bridges.CoreToRender.Swap();

        input.ReadFrom(Bridges.RenderToCore.InputBridge);

        WorldTick();
        long updateTime = CurrentElapsedTicks;
        Render();
        long renderTime = CurrentElapsedTicks;
        
        //avg tick time
        TimeSpan elapsedUpdateTime = Stopwatch.GetElapsedTime(0, updateTime);
        TimeSpan elapsedRenderTime = Stopwatch.GetElapsedTime(updateTime, renderTime);

        Interlocked.Exchange(ref tickUpdateTime, elapsedUpdateTime.TotalMicroseconds);
        Interlocked.Exchange(ref tickRenderTime, elapsedRenderTime.TotalMicroseconds);
    }

    private void WorldTick()
    {
        Camera.MainCamera.Tick();
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
