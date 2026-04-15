using System;

namespace Verse.Simulation;


public sealed class World
{
    internal World(string name)
    {
        this.Name = name;
        this.TileMapName = $"{name}_tile_map";
        this.TileMapName = $"{name}_tile_map_top";
    }

    public readonly string Name;

    public readonly string TileMapName;
    public readonly string TileMapNameTop;
    public void Render()
    {
        
    }
}