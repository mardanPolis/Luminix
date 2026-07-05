using System;
using System.Diagnostics;
using System.Threading;

namespace Luminix.Threading;

public class TickedThread : BaseThread
{
    //constructor
    protected TickedThread(uint tickRate = 60) : base()
    {
        TickRate = tickRate;
        threadAction = ThreadAction();
    }

    private Stopwatch stopwatch;
    private Stopwatch internalStopwatch;

    private long tickIntervalInTicks = 0;
    private long lastTickDate = 0;

    private uint tickRate = 0;
    private long tickCount = 0;
    private double tickTime = 0;
    private double tickDeltaTime = 0;
    private long tickAllocated = 0;

    protected long TickIntervalInTicks => tickIntervalInTicks;

    protected uint TickRate
    {
        get { return tickRate; }
        set { tickRate = value; Interlocked.Exchange(ref tickIntervalInTicks, (uint)(Stopwatch.Frequency / value)); }
    }
    protected long TickCount => tickCount;
    protected double TickTime => tickTime;
    protected double TickDeltaTime => tickDeltaTime;
    protected long TickAllocated => tickAllocated;

    protected long CurrentElapsedTicks => internalStopwatch.ElapsedTicks;
    public float PartialTick => (float)(stopwatch.ElapsedTicks - Volatile.Read(ref lastTickDate)) / Interlocked.Read(ref tickIntervalInTicks);

    protected virtual void Initialize() {}
    protected virtual void DebugInitialize() {}
    protected virtual void Tick() {}

    private Action ThreadAction()
    {
        return () =>
        {
            long startBytes = 0;
            long endBytes = 0;
            
            long nextTick = 0;

            long threshold10ms = (long)(10 * Stopwatch.Frequency / 1000.0);
            long threshold4ms = (long)(4 * Stopwatch.Frequency / 1000.0);
            long threshold1ms = (long)(1 * Stopwatch.Frequency / 1000.0);
            
            internalStopwatch = new Stopwatch();
            stopwatch = Stopwatch.StartNew();
    
            startBytes = GC.GetAllocatedBytesForCurrentThread();

            Initialize();
            if(Settings.Debug)
                DebugInitialize();
            
            endBytes = GC.GetAllocatedBytesForCurrentThread();

            Interlocked.Exchange(ref tickAllocated, endBytes - startBytes);

            logger.Log($"{Name} initialized in {stopwatch.Elapsed.Milliseconds}ms");
            logger.Log($"{Name} allocated {tickAllocated / 1024.0:F2} KB ({tickAllocated:N0} bytes during initialization!");

            Workspace.Current?.SetState(Workspace.State.Running);


            while (running)
            {
                long currentTicks = stopwatch.ElapsedTicks;

                if(currentTicks <= nextTick)
                {
                    long ticksRemaining = nextTick - currentTicks;
                    
                    if (ticksRemaining > threshold10ms)
                        Thread.Sleep(5);
                    else if (ticksRemaining > threshold4ms)
                        Thread.Yield();
                    else if (ticksRemaining > threshold1ms)
                        Thread.SpinWait(1);

                    continue;
                } 
                
                tickCount++;
                Interlocked.Exchange(ref lastTickDate, nextTick);
                nextTick += Interlocked.Read(ref tickIntervalInTicks);

                startBytes = GC.GetAllocatedBytesForCurrentThread();

                tickDeltaTime = internalStopwatch.Elapsed.TotalSeconds;
                
                internalStopwatch.Restart(); // NEW TICK

                Tick();

                tickAllocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;

                tickTime = internalStopwatch.Elapsed.TotalMicroseconds;
            }
        };
    }
}