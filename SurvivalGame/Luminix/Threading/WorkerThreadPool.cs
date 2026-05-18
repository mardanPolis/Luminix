using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Luminix.Threading;

public sealed class WorkerThreadPool
{
    public WorkerThreadPool(int workerThreadCount)
    {
        this.workerThreadCount = Math.Clamp(workerThreadCount, Settings.MinWorkerThreadCount, Settings.MaxWorkerThreadCount);;
        this.globalTasks = new BlockingCollection<Action>();
    }

    private int workerThreadCount;
    private BlockingCollection<Action> globalTasks;

    private List<WorkerThread> workerThreads;

    public int WorkerThreadCount => workerThreadCount;
    public IReadOnlyList<WorkerThread> WorkerThreads => workerThreads;

    public void Initialize()
    {
        workerThreads = new List<WorkerThread>(workerThreadCount);
        for (int i = 0; i < workerThreadCount; i++)
        {
            var worker = new WorkerThread(globalTasks, this);
            worker.Name = $"WorkerThread-{i}";
            workerThreads.Add(worker);
            worker.Start();
        }
    }

    public void Enqueue(Action work)
    {
        ArgumentNullException.ThrowIfNull(work, nameof(work));
        globalTasks.Add(work);
    }

    public void Shutdown()
    {
        foreach (var worker in workerThreads)
            worker.Stop();
    }


}