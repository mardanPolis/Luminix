using System;
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
        this.thread.IsBackground = true;
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

    private uint tickTime = 0;
    private uint avgTickTime = 0;

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

    public long TickCount => Interlocked.Read(ref tickCount);

    public float PartialTick
    {
        get 
        { 
            uint tickIntervalInTicks = this.tickIntervalInTicks;
            return (float)(stopwatch.ElapsedTicks % tickIntervalInTicks) / tickIntervalInTicks;
        }
    }

    public uint TickTimeInUS => tickTime;
    public uint AvgTickTimeInUS => avgTickTime;

    public void Start() => thread.Start();
    public void Stop() => running = false;

    void DebugRun()
    {
        Stopwatch internalStopWatch = new Stopwatch();
        stopwatch = Stopwatch.StartNew();
        Interlocked.Exchange(ref nextTick, stopwatch.ElapsedTicks);

        uint elapsedTimeSinceLastTick()
        {
            return (uint)(internalStopWatch.ElapsedTicks % tickIntervalInTicks);
        }

        uint[] tickTimes = new uint[500];
        uint currentIndex = 0;

        while(running)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            if(elapsedTicks < Interlocked.Read(ref nextTick))
            {
                Thread.SpinWait(25);
                continue;
            }
            
            currWriteStack = Bridges.CoreToRender.CurrentWriteStack;

            Interlocked.Increment(ref tickCount);
            Interlocked.Add(ref nextTick, tickIntervalInTicks);

            internalStopWatch.Restart();

            Update();
            Render();
            Optimize(tickIntervalInTicks, elapsedTimeSinceLastTick() / Stopwatch.Frequency);
            Bridges.CoreToRender.Swap();

            //get current tick time
            var tickTimeInTicks = elapsedTimeSinceLastTick();

            //store current tickTime
            tickTimes[currentIndex] = tickTimeInTicks;
            currentIndex = (uint)((currentIndex + 1) % tickTimes.Length);

            // avg tick time
            uint combinedTickTime = 0;
            foreach(var tickTime in tickTimes)
                combinedTickTime += tickTime;
            
            tickTime = (uint)(tickTimeInTicks * 0.1); // turning ticks into microseconds
            avgTickTime = combinedTickTime / (tickCount > tickTimes.Length ? (uint)tickTimes.Length : (uint)tickCount);
        }
    }

    void Run()
    {
        Stopwatch internalStopWatch = new Stopwatch();
        stopwatch = Stopwatch.StartNew();
        Interlocked.Exchange(ref nextTick, stopwatch.ElapsedTicks);

        uint elapsedTimeSinceLastTick()
        {
            return (uint)(internalStopWatch.ElapsedTicks % tickIntervalInTicks);
        }

        while(running)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            if(elapsedTicks < Interlocked.Read(ref nextTick))
            {
                Thread.SpinWait(25);
                continue;
            }
            
            currWriteStack = Bridges.CoreToRender.CurrentWriteStack;

            Interlocked.Increment(ref tickCount);
            Interlocked.Add(ref nextTick, tickIntervalInTicks);

            internalStopWatch.Restart();

            Update();
            Render();
            Optimize(tickIntervalInTicks, elapsedTimeSinceLastTick() / Stopwatch.Frequency);
            Bridges.CoreToRender.Swap();
        }
    }

    private void Update()
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
    private void Optimize(float maxTime, float time)
    {
        if(maxTime * 0.9f > time) currWriteStack.Optimize();
        else currWriteStack.SkipOptimization();
    }

}
