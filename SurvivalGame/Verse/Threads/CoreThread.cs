using System;
using System.Diagnostics;
using System.Threading;
using Verse.Rendering;

namespace Verse.Threads;

public sealed class CoreThread
{
    private Thread thread;

    uint tickRate = 0;
    uint tickIntervalTicks = 0;
    float tickIntervalSecond = 0;

    private bool running = true;
    public float PartialTick { get; private set; }
    public CoreThread(uint tickRate)
    {
        this.tickRate = tickRate;
        this.tickIntervalTicks = (uint)(TimeSpan.TicksPerSecond / tickRate);
        this.tickIntervalSecond = tickIntervalTicks / TimeSpan.TicksPerSecond;
        this.thread = new Thread(Run);
    }

    public void Start() => thread.Start();
    public void Stop() => running = false;

    void Run()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        long nextTick = stopwatch.ElapsedTicks;

        while(running)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            PartialTick = Math.Clamp(elapsedTicks % tickIntervalTicks / tickIntervalTicks, 0f, 1f);
            if(elapsedTicks >= nextTick)
            {
                nextTick += tickIntervalTicks;
                Tick(stopwatch);
            }
            else
            {
                Thread.SpinWait(50);
            }
        }
    }

    void Tick(Stopwatch stopwatch)
    {
        Update();
        Render(stopwatch.ElapsedTicks % tickIntervalTicks / TimeSpan.TicksPerSecond);
    }

    void Update()
    {
        
    }

    void Render(float timeElapsed)
    {
        RenderingSystem.Render(tickIntervalSecond, timeElapsed);
    }
}
