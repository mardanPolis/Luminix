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

    uint tickRate = 0;
    long tickIntervalTicks = 0;
    float tickIntervalSecond = 0;

    // debug variables
    private bool isDebug = false;
    double avgTickTime = 0;
    ulong tickCount = 0;

    public CoreThread(bool isDebug, uint tickRate)
    {   
        this.isDebug = isDebug;

        this.tickRate = tickRate;
        this.tickIntervalTicks = (uint)(TimeSpan.TicksPerSecond / tickRate);
        this.tickIntervalSecond = tickIntervalTicks / TimeSpan.TicksPerSecond;
        this.thread = new Thread(Run);
    }

    public void Start() => thread.Start();
    public void Stop() => running = false;

    void Run()
    {
        stopwatch = Stopwatch.StartNew();
        Stopwatch internalStopWatch = new Stopwatch();
        long nextTick = stopwatch.ElapsedTicks;

        float elapsedTimeSinceLastTick()
        {
            return internalStopWatch.ElapsedTicks % tickIntervalTicks / TimeSpan.TicksPerSecond;
        }

        if(!isDebug)
        {
            while(running)
            {
                long elapsedTicks = stopwatch.ElapsedTicks;
                if(elapsedTicks >= nextTick)
                {
                    nextTick += tickIntervalTicks;
                    internalStopWatch.Restart();
                    Update();
                    Render(elapsedTimeSinceLastTick());
                }
                else
                {
                    Thread.SpinWait(50);
                }
            }
        }
        else // debug mode
        {
            float[] tickTimes = new float[500];
            int currentIndex = 0;

            while(running)
            {
                long elapsedTicks = stopwatch.ElapsedTicks;
                if(elapsedTicks >= nextTick)
                {
                    nextTick += tickIntervalTicks;
                    Interlocked.Increment(ref tickCount);

                    internalStopWatch.Restart();

                    Update();
                    Render(elapsedTimeSinceLastTick());

                    tickTimes[currentIndex] = elapsedTimeSinceLastTick();
                    currentIndex = (currentIndex + 1) % tickTimes.Length;
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
        RenderingSystem.Render(tickIntervalSecond, timeElapsed);
    }

    public float PartialTick
    {
        get { return Math.Clamp(stopwatch.ElapsedTicks % tickIntervalTicks / tickIntervalTicks, 0f, 1f); }
    }
    public uint TickRate
    {
        set
        {
            this.tickRate = value;
            this.tickIntervalTicks = (uint)(TimeSpan.TicksPerSecond / value);
            this.tickIntervalSecond = tickIntervalTicks / TimeSpan.TicksPerSecond;
        }
        get
        {
            return tickRate;
        }
    }

    public bool IsDebug
    {
        get
        {
            return isDebug;
        }
    }
    public ulong TickCount
    {
        get
        {
            return tickCount; 
        }
    }
    public double AvgTickTime
    {
        get
        {
            return avgTickTime;
        }
    }
}
