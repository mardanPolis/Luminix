namespace Luminix.Simulation;

public sealed class World
{
    public World(string name, WorldComponent[] worldComponents)
    {
        this.Name = name;

        this.logger = Logger.Get();

        this.worldComponents = worldComponents;
    }

    public readonly string Name;
    private readonly Logger logger;


    private readonly WorldComponent[] worldComponents;

    public void Tick()
    {
        for(int i = 0; i < worldComponents.Length; i++)
        {
            worldComponents[i].Tick();
        }
    }
}