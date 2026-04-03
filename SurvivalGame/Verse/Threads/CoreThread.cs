using System;
using System.Diagnostics;
using System.Threading;
using Verse.Rendering;

namespace Verse.Threads;

internal sealed class CoreThread
{
    private Thread thread;
    private Stopwatch stopwatch;

    private volatile bool running = true;

    private ulong tickCount = 0;
    private volatile uint tickIntervalInTicks;

    // debug variables
    private readonly bool isDebug = false;

    private uint tickTime = 0;
    private uint avgTickTime = 0;

    public CoreThread(bool isDebug, uint tickRate)
    {   
        this.isDebug = isDebug;

        this.tickIntervalInTicks = (uint)(Stopwatch.Frequency / tickRate);
        if(isDebug)
            this.thread = new Thread(DebugRun);
        else
            this.thread = new Thread(Run);
    }

    public void Start() => thread.Start();
    public void Stop() => running = false;

    void DebugRun()
    {
        Stopwatch internalStopWatch = new Stopwatch();
        stopwatch = Stopwatch.StartNew();
        long nextTick = stopwatch.ElapsedTicks;

        uint elapsedTimeSinceLastTick()
        {
            return (uint)(internalStopWatch.ElapsedTicks % tickIntervalInTicks);
        }

        uint[] tickTimes = new uint[500];
        uint currentIndex = 0;

        while(running)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            if(elapsedTicks < nextTick)
            {
                Thread.SpinWait(50);
                continue;
            }
            
            nextTick += tickIntervalInTicks;
            Interlocked.Increment(ref tickCount);

            internalStopWatch.Restart();

            Update();
            
            RenderingSystem.DebugRender();
            RenderingSystem.SwapRenderStacks(tickIntervalInTicks, elapsedTimeSinceLastTick() / Stopwatch.Frequency);
            
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
            avgTickTime = combinedTickTime / (tickCount > (ulong)tickTimes.Length ? (uint)tickTimes.Length : (uint)tickCount);
        }
    }

    void Run()
    {
        Stopwatch internalStopWatch = new Stopwatch();
        stopwatch = Stopwatch.StartNew();
        long nextTick = stopwatch.ElapsedTicks;

        uint elapsedTimeSinceLastTick()
        {
            return (uint)(internalStopWatch.ElapsedTicks % tickIntervalInTicks);
        }

        while(running)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            if(elapsedTicks < nextTick)
            {
                Thread.SpinWait(25);
                continue;
            }

            nextTick += tickIntervalInTicks;
            Interlocked.Increment(ref tickCount);

            internalStopWatch.Restart();

            Update();
            
            RenderingSystem.Render();
            RenderingSystem.SwapRenderStacks(tickIntervalInTicks, elapsedTimeSinceLastTick() / Stopwatch.Frequency);
        }
    }

    void Update()
    {
        
    }

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

    public ulong TickCount => Interlocked.Read(ref tickCount);

    public float PartialTick
    {
        get 
        { 
            uint tickIntervalInTicks = this.tickIntervalInTicks;
            return Math.Clamp(
                (float)(stopwatch.ElapsedTicks % tickIntervalInTicks) / tickIntervalInTicks, 
                0f, 1f
            );
        }
    }

    public uint TickTimeInUS => tickTime;
    public uint AvgTickTimeInUS => avgTickTime;
}
