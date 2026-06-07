using Luminix.Collections;

namespace Luminix.Simulation;

public abstract class World
{
    public World()
    {
        this.Name = GetType().Name;

        this.logger = Logger.Get();
        this.worldComponents = [];
        this.worldCameras    = [];
    }
    public readonly string Name;
    public Camera MainCamera { get; set; }

    protected readonly Logger logger;
    protected readonly DenseList<WorldComponent> worldComponents;
    protected readonly DenseList<Camera> worldCameras;

    internal void RegisterCamera(Camera camera)
    {
        if(camera.IsDestroyed)
        {
            logger.Wrn("Tried to Register a destroyed camera!");
            return;
        }

        MainCamera ??= camera;
        worldCameras.Add(camera);
    }

    internal void DestroyCamera(Camera camera)
    {
        worldCameras.Remove(camera);
        if(MainCamera == camera)
            MainCamera = null;
        
        if(worldCameras.Count > 0)
            MainCamera = worldCameras[0];
    }

    public void Tick()
    {
        for(int i = 0; i < worldCameras.Count; i++)
        {
            worldCameras[i].Tick();
        }

        for(int i = 0; i < worldComponents.Count; i++)
        {
            worldComponents[i].Tick();
        }

        OnTick();
    }

    public virtual void OnTick()
    {
        
    }
}