using System;

namespace Luminix;

public abstract class EntitySystem
{
    protected Universe Universe { get; private set; }
    protected Graphics Graphics { get; private set; }
    protected EntityType Type { get; private set; } = null!;

    internal void Initialize(EntityType type)
    {
        if (Type != null)
            throw new InvalidOperationException("EntitySystem has already been initialized.");
        
        Graphics = Workspace.Current.GetService<Graphics>();
        Universe = Workspace.Current.GetService<Universe>();
        Type = type;
    }

    protected virtual void Tick() {}
}