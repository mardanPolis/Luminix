using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Workspaces;

namespace Luminix.Threading;

public class TickedThread : BaseThread
{
    //constructor
    protected TickedThread(uint tickRate = 60) : base()
    {
        TickRate = tickRate;
        threadAction = GetThreadAction();
    }

    private Stopwatch stopwatch;
    private Stopwatch internalStopwatch;

    private uint tickRate = 0;
    private long tickCount = 0;
    private long tickIntervalInTicks = 0;
    private long lastTickDate = 0;
    private double deltaTime = 0;

    private long allocated = 0;
    private double tickTime = 0;
    private double[] tickTimesBuffer1 = new double[128];
    private double[] tickTimesBuffer2 = new double[128];

    protected long TickIntervalInTicks => tickIntervalInTicks;
    protected long CurrentElapsedTicks => internalStopwatch.ElapsedTicks;

    protected long TickCount => tickCount;
    protected double TickDeltaTime => deltaTime;
    
    protected long TickAllocatedAmount => allocated;
    protected double TickTime => tickTime;
    protected double[] TickTimes => tickTimesBuffer1;

    public uint TickRate
    {
        get { return Volatile.Read(ref tickRate); }
        set { Volatile.Write(ref tickRate, tickRate); Interlocked.Exchange(ref tickIntervalInTicks, (uint)(Stopwatch.Frequency / value)); }
    }
    public float PartialTick => (float)(stopwatch.ElapsedTicks - Volatile.Read(ref lastTickDate)) / tickIntervalInTicks;

    protected virtual void Initialize() {}
    protected virtual void Tick() {}

    protected virtual void DebugInitialize() {}
    protected virtual void DebugTick() {}

    private Action GetThreadAction()
    {
        if(Settings.Debug)
            return DebugThreadAction();
        else
            return ThreadAction();
    }

    private Action DebugThreadAction()
    {
        return () =>
        {
            internalStopwatch = new Stopwatch();
            stopwatch = Stopwatch.StartNew();
    
            long startBytes = 0;
            long endBytes = 0;
            
            startBytes = GC.GetAllocatedBytesForCurrentThread();

            DebugInitialize();
            
            endBytes = GC.GetAllocatedBytesForCurrentThread();

            Interlocked.Exchange(ref allocated, endBytes - startBytes);

            logger.Log($"{Name} initialized in {stopwatch.Elapsed.Milliseconds}ms");
            logger.Log($"{Name} allocated {allocated / 1024.0:F2} KB ({allocated:N0} bytes during initialization!");

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

                DebugTick();

                endBytes = GC.GetAllocatedBytesForCurrentThread();

                Interlocked.Exchange(ref allocated, endBytes - startBytes);

                tickTimesBuffer2[currentIndex] = tickTime;

                Interlocked.Exchange(ref tickTime, internalStopwatch.Elapsed.TotalMicroseconds);

                currentIndex = Math.Clamp((int)(tickCount & 127), 0, tickTimesBuffer2.Length);
                tickTimesBuffer2[currentIndex] = tickTime;
            }
        };
    }
    private Action ThreadAction()
    {
        return () =>
        {
            internalStopwatch = new Stopwatch();
            stopwatch = Stopwatch.StartNew();

            long startBytes = 0;
            long endBytes = 0;
            
            startBytes = GC.GetAllocatedBytesForCurrentThread();

            Initialize();

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

                Tick();
            }
        };
    }
}