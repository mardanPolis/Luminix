using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Verse.Rendering;
using Verse.Simulation;
namespace Verse.Threads;

internal sealed class CoreThread
{
    public CoreThread(bool isDebug, uint tickRate)
    {   
        this.isDebug = isDebug;

        this.tickIntervalInTicks = (uint)(Stopwatch.Frequency / tickRate);
        if(isDebug)
            this.thread = new Thread(DebugRun);
        else
            this.thread = new Thread(Run);
        this.thread.IsBackground = false;
        this.thread.Priority = ThreadPriority.AboveNormal;

    }

    private Thread thread;
    private Stopwatch stopwatch;

    private volatile bool running = true;

    private long tickCount = 0;
    private volatile uint tickIntervalInTicks;

    private long nextTick = 0;

    private RenderStack currWriteStack = null;

    // debug variables
    private readonly bool isDebug = false;

    private long lastTickTime;

    private double deltaTime = 0;
    private double deltaTimeSec = 0;
    private double tickTime = 0;
    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;
    private double tickOptimizeTime = 0;

    public bool IsDebug
    {
        get
        {
            return isDebug;
        }
    }

    public uint TickRate
    {
        set { tickIntervalInTicks = (uint)(Stopwatch.Frequency / value); }
        get { return (uint)(Stopwatch.Frequency / tickIntervalInTicks); }
    }

    public long TickCount => Volatile.Read(ref tickCount);
    public double TickDeltaTimeSec => Volatile.Read(ref deltaTimeSec);

    public float PartialTick
    {
        get 
        { 
            long current = stopwatch.ElapsedTicks;
            long start = Interlocked.Read(ref lastTickTime);
            uint interval = this.tickIntervalInTicks;

            return (float)(current - start) / interval;
        }
    }

    public double TickTime => Volatile.Read(ref tickTime);
    public double TickDeltaTime => Volatile.Read(ref deltaTime);
    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);
    public double TickOptimizeTime => Volatile.Read(ref tickOptimizeTime);

    public void Start() => thread.Start();
    public void Stop() => running = false;

    void DebugRun()
    {
        Stopwatch internalStopwatch = new Stopwatch();
        stopwatch = Stopwatch.StartNew();
        Interlocked.Exchange(ref nextTick, stopwatch.ElapsedTicks);

        while(running)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            if(elapsedTicks < Interlocked.Read(ref nextTick))
            {
                Thread.SpinWait(25);
                continue;
            }
            Bridges.CoreToRender.Swap();

            Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalMicroseconds);
            Interlocked.Exchange(ref deltaTimeSec, internalStopwatch.Elapsed.TotalSeconds);
            
            internalStopwatch.Restart();

            Interlocked.Increment(ref tickCount);
            Interlocked.Exchange(ref lastTickTime, nextTick);
            Interlocked.Add(ref nextTick, tickIntervalInTicks);

            currWriteStack = Bridges.CoreToRender.CurrentWriteStack;

            Tick();
            long updateTime = internalStopwatch.ElapsedTicks;
            Render();
            long renderTime = internalStopwatch.ElapsedTicks;
            Optimize(tickIntervalInTicks, internalStopwatch.ElapsedTicks);
            long optimizeTime = internalStopwatch.ElapsedTicks;

            //avg tick time
            TimeSpan elapsedUpdateTime = Stopwatch.GetElapsedTime(0, updateTime);
            TimeSpan elapsedRenderTime = Stopwatch.GetElapsedTime(updateTime, renderTime);
            TimeSpan elapsedOptimizeTime = Stopwatch.GetElapsedTime(renderTime, optimizeTime);

            Interlocked.Exchange(ref tickTime, internalStopwatch.Elapsed.TotalMicroseconds);
            Interlocked.Exchange(ref tickUpdateTime, elapsedUpdateTime.TotalMicroseconds);
            Interlocked.Exchange(ref tickRenderTime, elapsedRenderTime.TotalMicroseconds);
            Interlocked.Exchange(ref tickOptimizeTime, elapsedOptimizeTime.TotalMicroseconds);
        }
    }

    void Run()
    {
        Stopwatch internalStopwatch = new Stopwatch();
        stopwatch = Stopwatch.StartNew();
        Interlocked.Exchange(ref nextTick, stopwatch.ElapsedTicks);

        while(running)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            if(elapsedTicks < Interlocked.Read(ref nextTick))
            {
                Thread.SpinWait(25);
                continue;
            }
            
            Bridges.CoreToRender.Swap();

            internalStopwatch.Stop();

            Interlocked.Exchange(ref deltaTimeSec, internalStopwatch.Elapsed.TotalSeconds);
            
            internalStopwatch.Restart();

            Interlocked.Increment(ref tickCount);
            Interlocked.Exchange(ref lastTickTime, nextTick);
            Interlocked.Add(ref nextTick, tickIntervalInTicks);

            currWriteStack = Bridges.CoreToRender.CurrentWriteStack;

            Tick();
            Render();
            Optimize(tickIntervalInTicks, internalStopwatch.ElapsedTicks);
        }
    }

    private void Tick()
    {
        Camera.MainCamera.Tick();
        
        foreach(var component in World.WorldComponents)
            component.Tick();
    }
    private void Render()
    {
        currWriteStack.Reset();
        
        currWriteStack.PostCameraData(Camera.MainCamera);
        
        foreach(var renderer in RendererRegistry.GetRenderers())
            renderer.RenderOn(currWriteStack);
    }
    private void Optimize(long maxTime, long time)
    {
        if(maxTime * 0.9 > time) currWriteStack.Optimize();
        else currWriteStack.SkipOptimization();
    }

}
