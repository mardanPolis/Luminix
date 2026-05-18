using System;
using System.Collections.Concurrent;
using System.Threading;
using Luminix.Identifiers;

namespace Luminix.Threading;

public class TaskedThread : BaseThread
{
    public TaskedThread(BlockingCollection<Action> globalTasks = null) : base()
    {
        this.globalTasks = globalTasks;
        this.internalTasks = new BlockingCollection<Action>();
        base.threadAction = Run;
    }

    private readonly BlockingCollection<Action> globalTasks;
    private readonly BlockingCollection<Action> internalTasks;

    public BlockingCollection<Action> GlobalTasks => globalTasks;
    public BlockingCollection<Action> InternalTasks => internalTasks;

    public void Do(Action task)
    {
        ArgumentNullException.ThrowIfNull(task, nameof(task));

        internalTasks.Add(task);
    }

    private readonly CancellationTokenSource cts = new CancellationTokenSource();

    protected override void OnStop()
    {
        cts.Cancel();
    }

    private void Run()
    {
        var collections = globalTasks != null
            ? new[] { globalTasks, internalTasks }
            : new[] { internalTasks };

        while (running)
        {
            try
            {
                BlockingCollection<Action>.TakeFromAny(collections, out var task, cts.Token);
                task?.Invoke();
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