namespace Luminix;

public abstract class GameObject
{
    public GameObject()
    {
        Workspace = Workspace.Current;
    }

    public Workspace Workspace { get; private init; }
    public virtual void Update() { }
    public virtual void Render() { }
}