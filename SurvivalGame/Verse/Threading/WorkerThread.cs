using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Verse.Collections;
using Verse.Identifiers;

namespace Verse.Threading;

public class WorkerThread
{
    //global values
    private static readonly ThreadLocal<WorkerThread> currentThread = new();
    private static readonly object lockObject = new();
    private static DenseList<WorkerThread> threads = new();

    public static WorkerThread CurrentThread => currentThread.Value;

    public static IEnumerable<WorkerThread> Threads => threads;

    //constructor
    public WorkerThread(Action init, Action update, uint tickRate = 60, bool isDebug = false)
    {
        TickRate = tickRate;

        if(isDebug)
            CreateDebugThread(init, update, out systemThread);
        else    
            CreateThread(init, update, out systemThread);

        lock (lockObject)
        {
            ThreadID = new ID((uint)threads.Count);
            threads.Add(this);
        }
    }
    protected WorkerThread(uint tickRate = 60, bool isDebug = false)
    {
        TickRate = tickRate;

        if(isDebug)
            CreateDebugThread(Initialize, OnDebugTick, out systemThread);
        else    
            CreateThread(Initialize, OnTick, out systemThread);

        lock (lockObject)
        {
            ThreadID = new ID((uint)threads.Count);
            threads.Add(this);
        }
    }

    //instance values
    private readonly Thread systemThread;
    private volatile bool running = true;
    
    private Stopwatch stopwatch;
    private Stopwatch internalStopwatch;

    private long tickCount = 0;
    private volatile uint tickIntervalInTicks = 0;

    private double deltaTime = 0;
    private long lastTickTime = 0;
    protected uint TickIntervalInTicks => tickIntervalInTicks;
    protected long CurrentElapsedTicks => internalStopwatch.ElapsedTicks;

    public ID ThreadID { get; private set; }
    public string Name { get => systemThread.Name; set => systemThread.Name = value; }
    public bool IsBackground { get => systemThread.IsBackground; set => systemThread.IsBackground = value; }
    public bool IsAlive => running;
    public ThreadPriority Priority { get => systemThread.Priority; set => systemThread.Priority = value; }

    public uint TickRate
    {
        set { tickIntervalInTicks = (uint)(Stopwatch.Frequency / value); }
        get { return (uint)(Stopwatch.Frequency / tickIntervalInTicks); }
    }
    public long TickCount => Volatile.Read(ref tickCount);
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
    public double TickDeltaTime => Volatile.Read(ref deltaTime);



    // debug variables
    private long allocated = 0;
    private double tickTime = 0;

    private double[] tickTimesBuffer1 = new double[128];
    private double[] tickTimesBuffer2 = new double[128];

    public double TickTime => Volatile.Read(ref tickTime);
    public long TickAllocatedAmount => Volatile.Read(ref allocated);

    public double[] TickTimes => tickTimesBuffer1;

    public void Start() => systemThread.Start();
    public void Stop() => running = false;

    protected virtual void Initialize() {}
    protected virtual void OnTick() {}
    protected virtual void OnDebugTick() {}

    private void CreateDebugThread(Action init, Action update, out Thread thread)
    {
        thread = new Thread(() =>
        {
            Logger.Initialize();
            currentThread.Value = this;

            init();

            try
            {
                long nextTick = 0;

                long startBytes = 0;
                long endBytes = 0;

                int currentIndex = 0;

                internalStopwatch = new Stopwatch();
                stopwatch = Stopwatch.StartNew();

                Interlocked.Exchange(ref nextTick, stopwatch.ElapsedTicks);
        
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
                    
                    var temp = tickTimesBuffer1;
                    tickTimesBuffer1 = tickTimesBuffer2;
                    tickTimesBuffer2 = temp;

                    internalStopwatch.Stop(); // OLD TICK STOP
                    
                    //Setting deltaTime
                    Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalSeconds);
                    
                    internalStopwatch.Restart(); // NEW TICK

                    Interlocked.Increment(ref tickCount);
                    
                    //Synchronization
                    Interlocked.Exchange(ref lastTickTime, nextTick);
                    Interlocked.Add(ref nextTick, tickIntervalInTicks);

                    startBytes = GC.GetAllocatedBytesForCurrentThread(); // allocated bytes at the start of the action

                    update();

                    endBytes = GC.GetAllocatedBytesForCurrentThread(); // allocated bytes at the end of the action

                    tickTimesBuffer2[currentIndex] = tickTime;

                    Interlocked.Exchange(ref allocated, endBytes - startBytes);
                    Interlocked.Exchange(ref tickTime, internalStopwatch.Elapsed.TotalMicroseconds);

                    currentIndex = Math.Clamp((int)(tickCount & 127), 0, tickTimesBuffer2.Length);
                    tickTimesBuffer2[currentIndex] = tickTime;

                }
            }
            finally
            {
                currentThread.Value = null;
            }
        });
    }
    private void CreateThread(Action init, Action update,out Thread thread)
    {
        thread = new Thread(() =>
        {
            Logger.Initialize();
            currentThread.Value = this;

            init();

            try
            {
                long nextTick = 0;

                internalStopwatch = new Stopwatch();
                stopwatch = Stopwatch.StartNew();

                nextTick = stopwatch.ElapsedTicks;
        
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
                    
                    internalStopwatch.Stop();

                    Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalSeconds);
                    
                    internalStopwatch.Restart();

                    Interlocked.Increment(ref tickCount);
                    Interlocked.Exchange(ref lastTickTime, nextTick);
                    nextTick += tickIntervalInTicks;

                    update();
                }
            }
            finally
            {
                currentThread.Value = null;
            }
        });
    }
}