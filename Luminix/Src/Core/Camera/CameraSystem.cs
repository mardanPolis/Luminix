namespace Luminix;

public class CameraSystem : EntitySystem
{
    private void Prepare(ref Camera camera)
    {
        Camera.SetPosition(ref camera, Camera.GetTargetPosition(in camera));
    }
}