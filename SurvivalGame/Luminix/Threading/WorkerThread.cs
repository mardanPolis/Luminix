using System;
using System.Collections.Concurrent;
using System.Threading;
using Luminix.Identifiers;

namespace Luminix.Threading;

public sealed class WorkerThread : BaseThread
{
    public WorkerThread(BlockingCollection<Action> globalTasks = null, WorkerThreadPool pool = null) : base()
    {
        this.globalWork = globalTasks;
        this.internalWork = new BlockingCollection<Action>();
        this.pool = pool;
        base.threadAction = Run;
        base.Name = $"{ThreadID} WorkerThread";
    }

    private readonly BlockingCollection<Action> globalWork;
    private readonly BlockingCollection<Action> internalWork;

    public BlockingCollection<Action> GlobalTasks => globalWork;
    public BlockingCollection<Action> InternalTasks => internalWork;

    private readonly WorkerThreadPool pool;
    public WorkerThreadPool Pool => pool;

    public void Do(Action work)
    {
        ArgumentNullException.ThrowIfNull(work, nameof(work));

        internalWork.Add(work);
    }

    private readonly CancellationTokenSource cts = new CancellationTokenSource();

    protected override void OnStop()
    {
        cts.Cancel();
    }

    private void Run()
    {
        var collections = globalWork != null
            ? new[] { globalWork, internalWork }
            : new[] { internalWork };

        while (running)
        {
            try
            {
                BlockingCollection<Action>.TakeFromAny(collections, out var work, cts.Token);
                work?.Invoke();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch(Exception ex)
            {
                Logger.Get().Err(ex);
            }
        }
    }
}