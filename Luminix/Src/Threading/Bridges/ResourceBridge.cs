using System;
using System.Collections.Frozen;
using System.Threading.Tasks;
using Luminix.Threading.Commands;

namespace Luminix.Threading.Bridges;

internal sealed class ResourceBridge
{
    private readonly TaskCommandBridge<ResourceCommand> commands = new();
    private FrozenDictionary<(Type, string), Resource> resources = null;
    private bool changedResources;

    public Task NewCommand(ResourceCommand command) => commands.NewCommand(command);

    public void ConsumeCommands(Action<ResourceCommand> action)
    {
        commands.ConsumeCommands(action);
    }

    public void SetResourceContainer(FrozenDictionary<(Type, string), Resource> resources)
    {
        this.resources = resources;
        changedResources = true;
    }

    public FrozenDictionary<(Type, string), Resource> GetResourceContainer()
    {
        changedResources = false;
        return resources;
    }

    public bool ResourceCommandsIncoming => commands.CommandsIncoming;
    public bool ChangedResources => changedResources;
}