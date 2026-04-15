namespace Verse.Simulation;

public abstract class WorldComponent
{
    protected static Universe ThisUniverse;
    public WorldComponent()
    {
        
    }

    public virtual void Initialize() { }
    public virtual void OnNewWorldInstance() {}
    public abstract void Tick();
}
