using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Workspaces;

namespace Luminix.Threading;

public class TickedThread : BaseThread
{
    //constructor
    public TickedThread(Action init, Action update, uint tickRate = 60)
    {
        TickRate = tickRate;
        threadAction = GetThreadAction(init, update);
    }
    protected TickedThread(uint tickRate = 60) : base()
    {
        TickRate = tickRate;
        threadAction = GetThreadAction();
    }

    private Stopwatch stopwatch;
    private Stopwatch internalStopwatch;

    private long tickCount = 0;
    private long tickIntervalInTicks = 0;
    private long lastTickDate = 0;
    private double deltaTime = 0;

    protected long TickIntervalInTicks => tickIntervalInTicks;
    protected long CurrentElapsedTicks => internalStopwatch.ElapsedTicks;

    public uint TickRate
    {
        get => (uint)(Stopwatch.Frequency / tickIntervalInTicks);
        set => tickIntervalInTicks = (uint)(Stopwatch.Frequency / value);
    }

    protected long UnsafeTickCount => tickCount;
    protected double UnsafeTickDeltaTime => deltaTime;

    public long TickCount => Volatile.Read(ref tickCount);
    public double TickDeltaTime => Volatile.Read(ref deltaTime);
    public float PartialTick => (float)(stopwatch.ElapsedTicks - Volatile.Read(ref lastTickDate)) / tickIntervalInTicks;


    private long initAllocated = 0;
    private long allocated = 0;
    private double tickTime = 0;
    private double[] tickTimesBuffer1 = new double[128];
    private double[] tickTimesBuffer2 = new double[128];

    public long TickAllocatedAmount => Volatile.Read(ref allocated);
    public double TickTime => Volatile.Read(ref tickTime);
    public double[] TickTimes => tickTimesBuffer1;

    protected virtual void Initialize() {}
    protected virtual void OnTick() {}
    protected virtual void OnDebugTick() {}

    private Action GetThreadAction(Action init, Action update)
    {
        if(Settings.Debug)
            return DebugThreadAction(init, update);
        else
            return ThreadAction(init, update);
    }

    private Action GetThreadAction()
    {
        if(Settings.Debug)
            return DebugThreadAction(Initialize, OnDebugTick);
        else
            return ThreadAction(Initialize, OnTick);
    }

    private Action DebugThreadAction(Action init, Action update)
    {
        return () =>
        {
            internalStopwatch = new Stopwatch();
            stopwatch = Stopwatch.StartNew();
    
            long startBytes = 0;
            long endBytes = 0;
            
            startBytes = GC.GetAllocatedBytesForCurrentThread();

            init();

            endBytes = GC.GetAllocatedBytesForCurrentThread();

            Interlocked.Exchange(ref allocated, endBytes - startBytes);

            logger.Log($"{Name} initialized in {stopwatch.Elapsed.Milliseconds}ms");
            logger.Log($"{Name} allocated {allocated / 1024.0:F2} KB ({allocated:N0} bytes during initialization!");

            initAllocated = endBytes;

            long nextTick = 0;
            int currentIndex = 0;

            Workspace.Current?.SetState(Workspace.State.Running);

            long threshold15ms = (long)(15 * Stopwatch.Frequency / 1000.0);
            long threshold4ms = (long)(4 * Stopwatch.Frequency / 1000.0);
            long threshold1ms = (long)(1 * Stopwatch.Frequency / 1000.0);

            while (running)
            {
                long currentTicks = stopwatch.ElapsedTicks;

                if(currentTicks <= nextTick)
                {
                    long ticksRemaining = nextTick - currentTicks;
                    
                    if (ticksRemaining > threshold15ms)
                        Thread.Sleep(10);
                    else if (ticksRemaining > threshold4ms)
                        Thread.Yield();
                    else if (ticksRemaining > threshold1ms)
                        Thread.SpinWait(1);

                    continue;
                } 
                
                //Setting deltaTime
                var temp = tickTimesBuffer1;
                tickTimesBuffer1 = tickTimesBuffer2;
                tickTimesBuffer2 = temp;

                Interlocked.Increment(ref tickCount);
                Interlocked.Exchange(ref lastTickDate, nextTick);
                nextTick += Interlocked.Read(ref tickIntervalInTicks);

                startBytes = GC.GetAllocatedBytesForCurrentThread();

                Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalSeconds);
                
                internalStopwatch.Restart(); // NEW TICK

                update();

                endBytes = GC.GetAllocatedBytesForCurrentThread();

                Interlocked.Exchange(ref allocated, endBytes - startBytes);

                tickTimesBuffer2[currentIndex] = tickTime;

                Interlocked.Exchange(ref tickTime, internalStopwatch.Elapsed.TotalMicroseconds);

                currentIndex = Math.Clamp((int)(tickCount & 127), 0, tickTimesBuffer2.Length);
                tickTimesBuffer2[currentIndex] = tickTime;

            }
        };
    }
    private Action ThreadAction(Action init, Action update)
    {
        return () =>
        {
            internalStopwatch = new Stopwatch();
            stopwatch = Stopwatch.StartNew();

            long startBytes = 0;
            long endBytes = 0;
            
            startBytes = GC.GetAllocatedBytesForCurrentThread();

            init();

            endBytes = GC.GetAllocatedBytesForCurrentThread();

            Interlocked.Exchange(ref allocated, endBytes - startBytes);

            logger.Log($"{Name} initialized in {stopwatch.Elapsed.Milliseconds}ms");
            logger.Log($"{Name} allocated {allocated / 1024.0:F2} KB ({allocated:N0} bytes during initialization!");

            long nextTick = 0;

            Workspace.Current?.SetState(Workspace.State.Running);

            long threshold15ms = (long)(15 * Stopwatch.Frequency / 1000.0);
            long threshold4ms = (long)(4 * Stopwatch.Frequency / 1000.0);
            long threshold1ms = (long)(1 * Stopwatch.Frequency / 1000.0);

            while (running)
            {
                long currentTicks = stopwatch.ElapsedTicks;
                if(currentTicks < nextTick)
                {
                    long ticksRemaining = nextTick - currentTicks;

                    if (ticksRemaining > threshold15ms)
                        Thread.Sleep(10);
                    else if (ticksRemaining > threshold4ms)
                        Thread.Yield();
                    else if (ticksRemaining > threshold1ms)
                        Thread.SpinWait(1);
                        
                    continue;
                }

                if(!(currentTicks > nextTick))
                    return;

                Interlocked.Increment(ref tickCount);
                Interlocked.Exchange(ref lastTickDate, nextTick);
                nextTick += Interlocked.Read(ref tickIntervalInTicks);

                Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalSeconds);
                internalStopwatch.Restart();

                update();
            }
        };
    }
}