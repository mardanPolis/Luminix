using System;
using Luminix.Identifiers;

namespace Luminix.Simulation;

public sealed class Map
{
    internal Map(World world, ID iD, ushort[] tileTypes)
    {
        this.World = world;
        this.ID = iD;
        this.tileTypes = tileTypes;
    }

    public readonly World World;
    public readonly ID ID;

    public readonly Camera Camera = new Camera();
    private readonly ushort[] tileTypes;
    
    public void Tick()
    {
        
    }

    public void Load()
    {
        
    }

    public void Save()
    {
        
    }
}
