using System;
using Verse.Rendering;

namespace Verse.Simulation;

public abstract class WorldComponent
{
    public WorldComponent()
    {
        World.NewWorldComponent(this);
    }

    public virtual void Initialize() { }
    public abstract void Tick();
}
