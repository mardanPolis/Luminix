using System;

namespace Luminix;

public abstract class EntitySystem
{
    protected Time Time { get; }
    protected World World { get; }
    protected Graphics Graphics { get; }
    protected AudioMedia AudioMedia { get; }
    protected EntityType Type { get; private set; } = null;

    public EntitySystem()
    {
        Time = Workspace.Current.GetService<Time>();
        World = Workspace.Current.GetService<World>();
        Graphics = Workspace.Current.GetService<Graphics>();
        AudioMedia = Workspace.Current.GetService<AudioMedia>();
    }

    internal void Initialize(EntityType type)
    {
        if (Type != null)
            throw new InvalidOperationException("EntitySystem has already been initialized.");

        Type = type;
    }

    protected virtual void Tick() {}
}