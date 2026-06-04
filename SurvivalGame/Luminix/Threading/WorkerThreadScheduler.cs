using System.Collections.Generic;
using System.Threading.Tasks;

namespace Luminix.Threading;

public sealed class WorkerThreadScheduler : TaskScheduler
{
    private readonly WorkerThreadPool pool;

    public WorkerThreadScheduler(WorkerThreadPool pool)
    {
        this.pool = pool;
    }

    protected override void QueueTask(Task task)
    {
        pool.Enqueue(() => TryExecuteTask(task));
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (BaseThread.CurrentThread != null && BaseThread.CurrentThread is WorkerThread)
            return TryExecuteTask(task);
        return false;
    }

    protected override IEnumerable<Task> GetScheduledTasks() => null;
}