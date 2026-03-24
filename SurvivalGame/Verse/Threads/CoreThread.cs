using System;
using System.Diagnostics;
using System.Threading;
using Verse.Rendering;

namespace Verse.Threads;

internal sealed class CoreThread
{
    private Thread thread;
    private volatile bool running = true;

    private Stopwatch stopwatch;

    private ulong tickCount = 0;
    private volatile uint tickIntervalInTicks;

    // debug variables
    private readonly bool isDebug = false;
    private uint tickTime = 0;
    private uint avgTickTime = 0;

    public CoreThread(bool isDebug, uint tickRate)
    {   
        this.isDebug = isDebug;

        this.tickIntervalInTicks = (uint)(TimeSpan.TicksPerSecond / tickRate);
        this.thread = new Thread(Run);
    }

    public void Start() => thread.Start();
    public void Stop() => running = false;

    void Run()
    {
        Stopwatch internalStopWatch = new Stopwatch();
        stopwatch = Stopwatch.StartNew();
        long nextTick = stopwatch.ElapsedTicks;

        uint elapsedTimeSinceLastTick()
        {
            return (uint)(internalStopWatch.ElapsedTicks % tickIntervalInTicks);
        }

        if(!isDebug)
        {
            while(running)
            {
                long elapsedTicks = stopwatch.ElapsedTicks;
                if(elapsedTicks >= nextTick)
                {
                    nextTick += tickIntervalInTicks;
                    Interlocked.Increment(ref tickCount);

                    internalStopWatch.Restart();

                    Update();
                    Render(elapsedTimeSinceLastTick() / TimeSpan.TicksPerSecond);
                }
                else
                {
                    Thread.SpinWait(50);
                }
            }
        }
        else // debug mode
        {
            uint[] tickTimes = new uint[500];
            uint currentIndex = 0;

            while(running)
            {
                long elapsedTicks = stopwatch.ElapsedTicks;
                if(elapsedTicks >= nextTick)
                {
                    nextTick += tickIntervalInTicks;
                    Interlocked.Increment(ref tickCount);

                    internalStopWatch.Restart();

                    Update();
                    Render(elapsedTimeSinceLastTick() / TimeSpan.TicksPerSecond);

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
                else
                {
                    Thread.SpinWait(50);
                }
            }
        }
    }

    void Update()
    {
        
    }

    void Render(float timeElapsed)
    {
        RenderingSystem.Render(tickIntervalInTicks, timeElapsed);
    }

    public float PartialTick
    {
        get 
        { 
            uint tickIntervalInTicks = this.tickIntervalInTicks;
            return Math.Clamp(stopwatch.ElapsedTicks % tickIntervalInTicks / tickIntervalInTicks, 0f, 1f);
        }
    }
    public uint TickRate
    {
        set { tickIntervalInTicks = (uint)(TimeSpan.TicksPerSecond / value); }
        get { return (uint)(TimeSpan.TicksPerSecond / tickIntervalInTicks); }
    }

    public bool IsDebug
    {
        get
        {
            return isDebug;
        }
    }
    public ulong TickCount => Interlocked.Read(ref tickCount);
    public uint TickTimeInUS => tickTime;
    public uint AvgTickTimeInUS => avgTickTime;
}
