namespace Luminix.Simulation;

public abstract class WorldComponent
{
    protected readonly Workspace Workspace;
    protected readonly Universe Universe;
    public WorldComponent()
    {
        Workspace = Workspace.Current;
        Workspace.TryGetService(out Universe);
    }

    public virtual void Initialize() { }
    public virtual void Tick() { }
}
