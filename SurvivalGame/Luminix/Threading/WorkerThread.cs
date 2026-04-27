using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Luminix.Collections;
using Luminix.Identifiers;
using Luminix.Workspaces;

namespace Luminix.Threading;

public class WorkerThread
{
    //global values
    private static readonly ThreadLocal<WorkerThread> thread = new(true);

    public static WorkerThread CurrentThread => thread.Value;
    public static IList<WorkerThread> Threads => thread.Values;

    //constructor
    public WorkerThread(Action init, Action update, uint tickRate = 60)
    {
        TickRate = tickRate;

        if(Settings.Debug)
            CreateDebugThread(init, update, out systemThread);
        else    
            CreateThread(init, update, out systemThread);

        ThreadID = new ID((uint)Threads.Count);
    }
    protected WorkerThread(uint tickRate = 60)
    {
        TickRate = tickRate;

        if(Settings.Debug)
            CreateDebugThread(Initialize, OnDebugTick, out systemThread);
        else    
            CreateThread(Initialize, OnTick, out systemThread);

        ThreadID = new ID((uint)Threads.Count);
    }


    // Core threading components
    private readonly Thread systemThread;
    private volatile bool running = true;

    public ID ThreadID { get; private set; }
    public string Name { get => systemThread.Name; set => systemThread.Name = value; }
    public bool IsBackground { get => systemThread.IsBackground; set => systemThread.IsBackground = value; }
    public bool IsAlive => running;
    public ThreadPriority Priority { get => systemThread.Priority; set => systemThread.Priority = value; }

    public Workspace Workspace { get; private set; }

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


    private long allocated = 0;
    private double tickTime = 0;
    private double[] tickTimesBuffer1 = new double[128];
    private double[] tickTimesBuffer2 = new double[128];

    public long TickAllocatedAmount => Volatile.Read(ref allocated);
    public double TickTime => Volatile.Read(ref tickTime);
    public double[] TickTimes => tickTimesBuffer1;

    public void Start() => systemThread.Start();
    public void Stop() => running = false;

    protected virtual void Initialize() {}
    protected virtual void OnTick() {}
    protected virtual void OnDebugTick() {}

    internal void AssignWorkspace(Workspace workspace)
    {
        if(Workspace != null && Workspace != workspace)
            throw new InvalidOperationException("Workspace for this thread already exists!");

        Workspace = workspace;
    } 

    private void CreateDebugThread(Action init, Action update, out Thread thread)
    {
        thread = new Thread(() =>
        {
            Logger.Initialize();
            WorkerThread.thread.Value = this;

            init();

            try
            {
                long nextTick = 0;

                long startBytes = 0;
                long endBytes = 0;

                int currentIndex = 0;

                internalStopwatch = new Stopwatch();
                stopwatch = Stopwatch.StartNew();
        
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

                    tickTimesBuffer2[currentIndex] = tickTime;

                    Interlocked.Exchange(ref allocated, endBytes - startBytes);
                    Interlocked.Exchange(ref tickTime, internalStopwatch.Elapsed.TotalMicroseconds);

                    currentIndex = Math.Clamp((int)(tickCount & 127), 0, tickTimesBuffer2.Length);
                    tickTimesBuffer2[currentIndex] = tickTime;

                }
            }
            finally
            {
                WorkerThread.thread.Value = null;
            }
        });
    }
    private void CreateThread(Action init, Action update,out Thread thread)
    {
        thread = new Thread(() =>
        {
            Logger.Initialize();
            WorkerThread.thread.Value = this;

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
                    
                    Interlocked.Exchange(ref deltaTime, internalStopwatch.Elapsed.TotalSeconds);
                    
                    internalStopwatch.Restart();

                    Interlocked.Increment(ref tickCount);
                    Interlocked.Exchange(ref lastTickDate, nextTick);
                    Interlocked.Add(ref nextTick, tickIntervalInTicks);

                    update();
                }
            }
            finally
            {
                WorkerThread.thread.Value = null;
            }
        });
    }
}