using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Luminix.Threading.Bridges;

//Fire and forget
internal sealed class CommandBridge<TCommand>
{
    private readonly ConcurrentQueue<TCommand> commands = new();

    public void NewCommand(TCommand command)
    {
        commands.Enqueue(command);
    }

    public void ConsumeCommands(Action<TCommand> action)
    {
        while(commands.TryDequeue(out TCommand command))
        {
            action(command);
        }
    }

    public void ClearCommands() => commands.Clear();

    public bool CommandsIncoming => !commands.IsEmpty;
}

//Fire and wait
internal sealed class TaskCommandBridge<TCommand>
{
    private readonly ConcurrentQueue<(TaskCompletionSource, TCommand)> commands = new();

    public Task NewCommand(TCommand command)
    {
        var tcs = new TaskCompletionSource();
        commands.Enqueue((tcs, command));
        return tcs.Task;
    }

    public void ConsumeCommands(Action<TCommand> action)
    {
        while(commands.TryDequeue(out (TaskCompletionSource, TCommand) item))
        {
            var (tcs, command) = item;
            try
            {
                action(command);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }

    public void ClearCommands() => commands.Clear();

    public bool CommandsIncoming => !commands.IsEmpty;
}

//Fire and wait
internal sealed class TaskCommandBridge<TCommand, TResponse>
{
    private readonly ConcurrentQueue<(TaskCompletionSource<(TCommand, TResponse)>, TCommand)> commands = new();

    public Task<(TCommand, TResponse)> NewCommand(TCommand command)
    {
        var tcs = new TaskCompletionSource<(TCommand, TResponse)>();
        commands.Enqueue((tcs, command));
        return tcs.Task;
    }

    public void ConsumeCommands(Func<TCommand, TResponse> func)
    {
        while(commands.TryDequeue(out (TaskCompletionSource<(TCommand, TResponse)> tcs, TCommand) item))
        {
            var (tcs, command) = item;
            try
            {
                var result = func(command);
                tcs.SetResult((command, result));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }
    
    public void ClearCommands() => commands.Clear();

    public bool CommandsIncoming => !commands.IsEmpty;
}