using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Threading.Tasks;
using Luminix.Threading.Commands;

namespace Luminix.Threading.Bridges;

internal sealed class ResourceBridge
{
    private readonly TaskCommandBridge<ResourceCommand> commands = new();
    private readonly ConcurrentQueue<TaskCompletionSource<FrozenDictionary<(Type, string), Resource>>> requests = new();
    private volatile FrozenDictionary<(System.Type, string), Resource> resources = null;
    private volatile bool changedResources;


    public Task NewCommand(ResourceCommand command) => commands.NewCommand(command);

    public void ConsumeCommands(Action<ResourceCommand> action)
    {
        commands.ConsumeCommands(action);
    }

    public void ConsumeRequests(FrozenDictionary<(Type, string), Resource> resources)
    {
        while(requests.TryDequeue(out TaskCompletionSource<FrozenDictionary<(Type, string), Resource>> task))
        {
            task.SetResult(resources);
        }
    } 

    public void SetResourceContainer(FrozenDictionary<(System.Type, string), Resource> resources)
    {
        this.resources = resources;
        changedResources = true;
        ConsumeRequests(resources);
    }

    public FrozenDictionary<(System.Type, string), Resource> GetResourceContainer()
    {
        if(resources == null)
        {
            var tcs = new TaskCompletionSource<FrozenDictionary<(Type, string), Resource>>();
            requests.Enqueue(tcs);
            tcs.Task.GetAwaiter().GetResult();
        }

        changedResources = false;
        return resources;
    }

    public bool ResourceCommandsIncoming => commands.CommandsIncoming;
    public bool ResourcesRequestsIncoming => requests.Count > 0;
    public bool ChangedResources => changedResources;
}