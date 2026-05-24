using System;
using System.Collections.Concurrent;
using System.Linq;
using Luminix.Resources;

namespace Luminix.Threading.Bridges;

internal class ResourceBridge
{
    public BlockingCollection<Resource> Appended { get; } = new();
    public BlockingCollection<Resource> Removed { get; } = new();

    public void Consume(Action<Resource> onAppended, Action<Resource> onRemoved)
    {
        ConsumeAppended(onAppended);
        ConsumeRemoved(onRemoved);
    }

    public void ConsumeAppended(Action<Resource> action)
    {
        while (Appended.TryTake(out var resource))
            action(resource);
    }
    public void ConsumeRemoved(Action<Resource> action)
    {
        while (Removed.TryTake(out var resource))
            action(resource);
    }

    public Resource TakeAppended()
    {
        return Appended.Take();
    }
    public Resource TakeRemoved()
    {
        return Appended.Take();
    }
    
    public void Publish(Resource resource) => Appended.Add(resource);
    public void Revoke(Resource resource) => Removed.Add(resource);
}
