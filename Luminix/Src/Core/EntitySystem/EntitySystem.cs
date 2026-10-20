using System;

namespace Luminix;

public abstract class EntitySystem
{
    protected Workspace Workspace { get; }
    protected Time Time { get; }
    protected World World { get; }
    protected Graphics Graphics { get; }
    protected AudioMedia AudioMedia { get; }
    protected EntityType Type { get; private set; } = null;

    public EntitySystem()
    {
        Workspace = Workspace.Current;
        Time = Workspace.GetService<Time>();
        World = Workspace.GetService<World>();
        Graphics = Workspace.GetService<Graphics>();
        AudioMedia = Workspace.GetService<AudioMedia>();
    }

    internal void Initialize(EntityType type)
    {
        if (Type != null)
            throw new InvalidOperationException("EntitySystem has already been initialized.");

        Type = type;
    }

    protected virtual void Tick() {}
}