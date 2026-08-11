using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Luminix.Threading;

public sealed class WorkerThreadPool : Workspace.Service
{
    public WorkerThreadPool(int workerThreadCount)
    {
        this.workerThreadCount = Math.Clamp(workerThreadCount, 2, 5); //TODO : customizable thread counts
        this.globalTasks = new BlockingCollection<Action>();
    }

    private int workerThreadCount;
    private BlockingCollection<Action> globalTasks;

    private List<WorkerThread> workerThreads;

    public int WorkerThreadCount => workerThreadCount;
    public IReadOnlyList<WorkerThread> WorkerThreads => workerThreads;

    protected override void Start()
    {
        workerThreads = new List<WorkerThread>(workerThreadCount);
        for (int i = 0; i < workerThreadCount; i++)
        {
            var worker = new WorkerThread(globalTasks, this);
            worker.Name = $"WorkerThread-{i}";
            worker.Priority = System.Threading.ThreadPriority.Normal;
            workerThreads.Add(worker);
            worker.Start();
        }

        //TODO : if(Settings.DebugInitialization)
        Logger.Get().Log($"Initialized {workerThreads.Count} worker threads.");
    }

    public void Enqueue(Action work)
    {
        ArgumentNullException.ThrowIfNull(work, nameof(work));
        globalTasks.Add(work);
    }
    public void ParallelFor(int length, Action<int> work)
    {
        if (length <= 0) return;
        if (length == 1) { work(0); return; }

        int chunkSize = (length + workerThreadCount) / (workerThreadCount + 1);
        int chunkCount = (length + chunkSize - 1) / chunkSize;

        using var countdown = new CountdownEvent(chunkCount);
        var exceptions = new ConcurrentBag<Exception>();

        for (int i = 0; i < chunkCount; i++)
        {
            int start = i * chunkSize;
            int end = Math.Min(start + chunkSize, length);

            Enqueue(() =>
            {
                try
                {
                    for (int j = start; j < end; j++)
                        work(j);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    countdown.Signal();
                }
            });
        }

        if (globalTasks.TryTake(out var stolenTask))
            stolenTask();

        countdown.Wait();

        if (!exceptions.IsEmpty)
            throw new AggregateException(exceptions);
    }
    
    public void ParallelFor<T>(IReadOnlyCollection<T> items, Action<T> work)
    {
        if (items.Count == 0) return;

        using var countdown = new CountdownEvent(items.Count);
        var exceptions = new ConcurrentBag<Exception>();

        foreach (var item in items)
        {
            var captured = item;
            Enqueue(() =>
            {
                try
                {
                    work(captured);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    countdown.Signal();
                }
            });
        }


        if(items.Count > workerThreadCount)
        {
            while (!countdown.IsSet)
            {
                if (globalTasks.TryTake(out var stolenTask))
                    stolenTask();
                else
                    countdown.Wait(1);
            }
        }
        else
            countdown.Wait();

        if (!exceptions.IsEmpty)
            throw new AggregateException(exceptions);
    }

    public Task ParallelForAsync<T>(IReadOnlyCollection<T> items, Action<T> work)
    {
        if (items.Count == 0) return Task.CompletedTask;

        var exceptions = new ConcurrentBag<Exception>();
        var countdown  = new CountdownEvent(items.Count);
        var tcs        = new TaskCompletionSource();

        foreach (var item in items)
        {
            var captured = item;
            Enqueue(() =>
            {
                try   { work(captured); }
                catch (Exception ex) { exceptions.Add(ex); }
                finally
                {
                    if (countdown.Signal() && countdown.IsSet)
                    {
                        countdown.Dispose();
                        if (exceptions.IsEmpty) tcs.SetResult();
                        else tcs.SetException(new AggregateException(exceptions));
                    }
                }
            });
        }

        return tcs.Task;
    }
    
    public Task ParallelForAsync<T>(IReadOnlyCollection<T> items, Func<T, Task> work)
    {
        if (items.Count == 0) return Task.CompletedTask;

        var exceptions = new ConcurrentBag<Exception>();
        var countdown  = new CountdownEvent(items.Count);
        var tcs        = new TaskCompletionSource();
        var scheduler  = new WorkerThreadScheduler(this);

        foreach (var item in items)
        {
            var captured = item;
            Enqueue(() =>
            {
                var t = work(captured).ContinueWith(_ => { }, scheduler);
                t.ContinueWith(prev =>
                {
                    if (prev.IsFaulted) exceptions.Add(prev.Exception!);
                    if (countdown.Signal() && countdown.IsSet)
                    {
                        countdown.Dispose();
                        if (exceptions.IsEmpty) tcs.SetResult();
                        else tcs.SetException(new AggregateException(exceptions));
                    }
                }, scheduler);
            });
        }

        return tcs.Task;
    }
    public void Shutdown()
    {
        foreach (var worker in workerThreads)
            worker.Stop();
    }
}