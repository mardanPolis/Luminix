namespace Luminix.Simulation;

public class CameraComponent
{
    protected readonly Workspace Workspace;
    protected readonly Universe Universe;
    public CameraComponent(Camera camera)
    {
        Workspace = Workspace.Current;
        Workspace.TryGetService(out Universe);
        
        this.camera = camera;
        this.camera.AttachComponent(this);
    }
    protected readonly Camera camera;

    public virtual void Tick() { }
}