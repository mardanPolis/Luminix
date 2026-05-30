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
    private volatile uint tickIntervalInTicks = 0;
    private long lastTickDate = 0;
    private double deltaTime = 0;

    protected uint TickIntervalInTicks => tickIntervalInTicks;
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
            logger.Log($"{Name} allocated {allocated} bytes during initialization!");

            initAllocated = endBytes;

            long nextTick = 0;

            int currentIndex = 0;

            Workspace.Current?.SetState(Workspace.State.Running);

            while (running)
            {
                long currentTicks = stopwatch.ElapsedTicks;
                long targetTick = Interlocked.Read(ref nextTick);

                if(currentTicks < targetTick)
                {
                    long ticksRemaining = nextTick - currentTicks;
                    double msRemaining = (double)ticksRemaining / Stopwatch.Frequency * 1000;
                    
                    if (msRemaining > 15)
                        Thread.Sleep(10);
                    else if (msRemaining > 4)
                        Thread.Yield();
                    else if (msRemaining > 1)
                        Thread.SpinWait(5);

                    continue;
                } 
                
                //Setting deltaTime
                Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalSeconds);
                
                var temp = tickTimesBuffer1;
                tickTimesBuffer1 = tickTimesBuffer2;
                tickTimesBuffer2 = temp;

                internalStopwatch.Restart(); // NEW TICK

                Interlocked.Increment(ref tickCount);
                Interlocked.Exchange(ref lastTickDate, nextTick);
                Interlocked.Add(ref nextTick, tickIntervalInTicks);

                startBytes = GC.GetAllocatedBytesForCurrentThread();

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

            init();

            long nextTick = 0;

            nextTick = stopwatch.ElapsedTicks;
    
            Workspace.Current?.SetState(Workspace.State.Running);

            while (running)
            {
                long currentTicks = stopwatch.ElapsedTicks;
                long targetTick = Interlocked.Read(ref nextTick);

                if(currentTicks < targetTick)
                {
                    long ticksRemaining = nextTick - currentTicks;
                    double msRemaining = (double)ticksRemaining / Stopwatch.Frequency * 1000;

                    if (msRemaining > 15)
                        Thread.Sleep(10);
                    else if (msRemaining > 4)
                        Thread.Yield();
                    else if (msRemaining > 1)
                        Thread.SpinWait(5);
                        
                    continue;
                }
                
                Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalSeconds);
                
                internalStopwatch.Restart();

                Interlocked.Increment(ref tickCount);
                Interlocked.Exchange(ref lastTickDate, nextTick);
                Interlocked.Add(ref nextTick, tickIntervalInTicks);

                update();
            }
        };
    }
}