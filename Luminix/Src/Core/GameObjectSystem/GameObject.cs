namespace Luminix;

public abstract class GameObject
{
    public GameObject()
    {
        Workspace = Workspace.Current;
    }

    public Workspace Workspace { get; private init; }
    protected virtual void Update() { }
    protected virtual void Render() { }
}