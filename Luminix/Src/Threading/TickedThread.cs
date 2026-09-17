using System;
using System.Diagnostics;
using System.Threading;

namespace Luminix.Threading;

public sealed class TickedThread : ManagedThread
{
    //constructor
    public TickedThread(string name, Action init, Action tick, Action onStop, uint tickRate = 60) : base(name)
    {
        threadAction = () =>
        {
            bool isNoGCRegion = GC.TryStartNoGCRegion(5000000);
                
            var Time = new Time();
            Time.TickRate = tickRate;
            TickRate = Time.TickRate;

            long startBytes = 0;
            long endBytes = 0;
            
            long nextTick = 0;

            long threshold10ms = (long)(10 * Stopwatch.Frequency / 1000.0);
            long threshold4ms = (long)(4 * Stopwatch.Frequency / 1000.0);
            long threshold1ms = (long)(1 * Stopwatch.Frequency / 1000.0);
            
            internalStopwatch = new Stopwatch();
            stopwatch = Stopwatch.StartNew();
    
            startBytes = GC.GetAllocatedBytesForCurrentThread();

            init();
            
            Workspace.Current.Start();

            endBytes = GC.GetAllocatedBytesForCurrentThread();

            Time.TickAllocated = endBytes - startBytes;

            Logger.NewLog(Logger.Log.Level.Info, $"{Name} initialized in {stopwatch.Elapsed.Milliseconds}ms");
            Logger.NewLog(Logger.Log.Level.Info, $"{Name} allocated {Time.TickAllocated / 1024.0:F2} KB | ({Time.TickAllocated:N0} bytes) during initialization!");

            long before = GC.GetTotalMemory(true); // exits NoGCRegion automatically
            GC.Collect();
            long after = GC.GetTotalMemory(true);

            long cleared = before - after;
            Logger.NewLog(Logger.Log.Level.Info, $"GC cleared: {cleared / 1024.0:F2} KB | ({cleared:N0} bytes)");

            while (running)
            {
                long currentTicks = stopwatch.ElapsedTicks;

                if(currentTicks <= nextTick)
                {
                    long ticksRemaining = nextTick - currentTicks;
                    
                    if (ticksRemaining > threshold10ms)
                        System.Threading.Thread.Sleep(5);
                    else if (ticksRemaining > threshold4ms)
                        System.Threading.Thread.Yield();
                    else if (ticksRemaining > threshold1ms)
                        System.Threading.Thread.SpinWait(1);

                    continue;
                } 
                
                Time.TickCount++;
                Interlocked.Exchange(ref lastTickDate, nextTick);
                nextTick += Interlocked.Read(ref tickIntervalInTicks);

                Time.TickDeltaTime = internalStopwatch.Elapsed.TotalSeconds;
                
                internalStopwatch.Restart(); // NEW TICK

                startBytes = GC.GetAllocatedBytesForCurrentThread();

                tick();

                Time.TickAllocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;

                Time.TickTime = internalStopwatch.Elapsed.TotalSeconds;

                if(tickRate != Time.TickRate)
                {
                    tickRate = Time.TickRate;
                    TickRate = tickRate;
                }
            }
            
            onStop();
        };
    }

    private long TickRate { set => Interlocked.Exchange(ref tickIntervalInTicks, (uint)(Stopwatch.Frequency / value)); }

    private Stopwatch stopwatch;
    private Stopwatch internalStopwatch;

    private long tickIntervalInTicks = 0;
    private long lastTickDate = 0;

    public long CurrentElapsedTicks => internalStopwatch.ElapsedTicks;
    public float PartialTick => (float)(stopwatch.ElapsedTicks - Volatile.Read(ref lastTickDate)) / Interlocked.Read(ref tickIntervalInTicks);
}