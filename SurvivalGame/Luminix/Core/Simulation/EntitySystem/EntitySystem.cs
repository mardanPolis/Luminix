using System;

namespace Luminix;

public abstract class EntitySystem
{
    public abstract Type[] GetComponentTypes();

    protected Graphics Graphics { get; private set; }
    protected EntityType Type { get; private set; } = null!;

    internal void Initialize(EntityType type)
    {
        if (Type != null)
            throw new InvalidOperationException("EntitySystem has already been initialized.");
        
        Graphics = Workspace.Current.GetService<Graphics>();
        Type = type;
    }
}