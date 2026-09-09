using System;
using System.Threading;
using System.Threading.Tasks;
using Luminix.Threading.Commands;

namespace Luminix.Threading.Bridges;

internal sealed class WindowBridge
{
    private readonly CommandBridge<WindowCommand> commands = new();
    private readonly TaskCommandBridge<WindowCreateCommand, WindowCreateCommandResponse> createCommands = new();
    private readonly CommandBridge<WindowSyncCommand> syncCommands = new();

    private readonly ManualResetEventSlim firstCreateCommandReceived = new(initialState: false);

    public void NewCommand(WindowCommand command) => commands.NewCommand(command);
    public Task<(WindowCreateCommand, WindowCreateCommandResponse)> NewCommand(WindowCreateCommand command)
    {
        var task = createCommands.NewCommand(command);
        firstCreateCommandReceived.Set();
        return task;
    }
    public void NewCommand(WindowSyncCommand command) => syncCommands.NewCommand(command);

    public void ConsumeCommands(Action<WindowCommand> action)
    {
        commands.ConsumeCommands(action);
    }
    public void ConsumeCreateCommands(Func<WindowCreateCommand, WindowCreateCommandResponse> action)
    {
        firstCreateCommandReceived.Wait();
        createCommands.ConsumeCommands(action);
    }

    public void ConsumeSyncCommand(Action<WindowSyncCommand> action)
    {
        syncCommands.ConsumeCommands(action);
    }

    public void ClearSyncCommands()
    {
        syncCommands.ClearCommands();
    }

    public bool CommandsIncoming => commands.CommandsIncoming;
    public bool CreateCommandIncoming => createCommands.CommandsIncoming;
    public bool SyncCommandIncoming => syncCommands.CommandsIncoming;
}