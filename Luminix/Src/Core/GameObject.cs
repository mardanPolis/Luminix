namespace Luminix;

public abstract class GameObject
{
    public GameObject()
    {
        Workspace = Workspace.Current;
        Time = Workspace.GetService<Time>();
        World = Workspace.GetService<World>();
        Graphics = Workspace.GetService<Graphics>();
        AudioMedia = Workspace.GetService<AudioMedia>();
    }

    public Workspace Workspace { get; }
    protected Time Time { get; }
    protected World World { get; }
    protected Graphics Graphics { get; }
    protected AudioMedia AudioMedia { get; }
    
    protected virtual void Update() { }
    protected virtual void Render() { }
}