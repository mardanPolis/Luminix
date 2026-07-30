using System;

namespace Luminix;

public abstract class EntitySystem
{
    public abstract Type[] GetComponentTypes();
    protected EntityType Type { get; private set; } = null!;

    internal void Initialize(EntityType type)
    {
        if (Type != null)
            throw new InvalidOperationException("EntitySystem has already been initialized.");
            
        Type = type;
    }
}