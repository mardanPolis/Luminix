using System;
using System.Diagnostics;
using System.Threading;
using Verse.Simulation;
using Verse.Rendering;

namespace Verse.Threading;

internal sealed class CoreThread : WorkerThread
{
    public CoreThread(uint tickRate, bool isDebug) : base(tickRate, isDebug)
    {   

        this.Name = "CoreThread";
        this.IsBackground = false;
        this.Priority = ThreadPriority.AboveNormal;
    }

    private RenderStack currWriteStack = null;

    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;
    private double tickOptimizeTime = 0;

    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);
    public double TickOptimizeTime => Volatile.Read(ref tickOptimizeTime);

    private Input input;
    private Random random = new Random();

    protected sealed override void Initialize()
    {
        Input.Initialize();
        input = Input.Get();
    } 

    protected sealed override void OnTick()
    {
        Bridges.CoreToRender.Swap();
        currWriteStack = Bridges.CoreToRender.CurrentWriteStack;

        input.ReadFrom(Bridges.RenderToCore.InputBridge);

        Tick();
        Render();
        Optimize(TickIntervalInTicks, CurrentElapsedTicks);
    }

    protected sealed override void OnDebugTick()
    {
        Bridges.CoreToRender.Swap();
        currWriteStack = Bridges.CoreToRender.CurrentWriteStack;

        input.ReadFrom(Bridges.RenderToCore.InputBridge);

        Tick();
        long updateTime = CurrentElapsedTicks;
        Render();
        long renderTime = CurrentElapsedTicks;
        Optimize(TickIntervalInTicks, CurrentElapsedTicks);
        long optimizeTime = CurrentElapsedTicks;
        
        //avg tick time
        TimeSpan elapsedUpdateTime = Stopwatch.GetElapsedTime(0, updateTime);
        TimeSpan elapsedRenderTime = Stopwatch.GetElapsedTime(updateTime, renderTime);
        TimeSpan elapsedOptimizeTime = Stopwatch.GetElapsedTime(renderTime, optimizeTime);

        Interlocked.Exchange(ref tickUpdateTime, elapsedUpdateTime.TotalMicroseconds);
        Interlocked.Exchange(ref tickRenderTime, elapsedRenderTime.TotalMicroseconds);
        Interlocked.Exchange(ref tickOptimizeTime, elapsedOptimizeTime.TotalMicroseconds);
    }

    private void Tick()
    {
        Camera.MainCamera.Tick();
        
        for(int i = 0; i < World.WorldComponents.Count; i++)
            World.WorldComponents[i].Tick();
    }
    private void Render()
    {
        currWriteStack.Reset();
        
        currWriteStack.PostCameraData(Camera.MainCamera);
        
        var renderers = RendererRegistry.GetRenderers();

        for(int i = 0; i < renderers.Length; i++)
            renderers[i].RenderOn(currWriteStack);
    }
    private void Optimize(long maxTime, long time)
    {
        if(maxTime * 0.9 > time) currWriteStack.Optimize();
        else currWriteStack.SkipOptimization();
    }

}
