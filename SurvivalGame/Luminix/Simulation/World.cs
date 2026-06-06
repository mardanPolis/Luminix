using Luminix.Collections;

namespace Luminix.Simulation;

public abstract class World
{
    public World(string name) : this(name, null) {}
    public World(string name, WorldComponent[] worldComponents)
    {
        this.Name = name;

        this.logger = Logger.Get();
        this.worldComponents = [.. worldComponents];
    }

    public readonly string Name;
    private readonly Logger logger;

    public Camera MainCamera { get; set; }

    private readonly DenseList<WorldComponent> worldComponents;

    public void Tick()
    {
        MainCamera.Tick();
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