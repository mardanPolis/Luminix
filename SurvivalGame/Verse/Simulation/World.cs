using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Verse.Identifiers;

namespace Verse.Simulation;

public sealed class World
{
    internal World(string name, int mapWidth, int mapHeight, WorldComponent[] worldComponents, bool debug)
    {
        this.Name = name;
        this.debug = debug;

        this.logger = Logger.Get();

        this.worldComponents = worldComponents;

        this.MapWidth = mapWidth;
        this.MapHeight = mapHeight;

        Vector2[] tilePositions = new Vector2[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                tilePositions[x + mapWidth * y] = new Vector2(x, y);

        this.tilePositions = tilePositions;

        this.maps = new List<Map>();
        this.mapTileTypes = new List<ushort[]>();
        this.mapsById = new Dictionary<ID, Map>();
    }

    public readonly string Name;
    private readonly bool debug;
    private readonly Logger logger;


    private readonly WorldComponent[] worldComponents;

    public readonly int MapWidth, MapHeight;
    private readonly Vector2[] tilePositions;

    private readonly List<Map> maps;
    private readonly List<ushort[]> mapTileTypes;
    private readonly Dictionary<ID, Map> mapsById;
    private int activeMapIndex = 0;
    private ID activeMapID = new ID(0);
    private ID lastMapID = new ID(0);

    public void Tick()
    {
        if(maps.Count <= 0)
            return;

        if(activeMapIndex == 0)
        {
            logger.Wrn("No Map is active");
            return;
        }

        for(int i = 0; i < worldComponents.Length; i++)
            worldComponents[i].Tick(maps[activeMapIndex]);

        maps[activeMapIndex].Tick();
    }

    public void GenerateBlankMap()
    {
        var mapID = new ID(lastMapID.Value + 1);
        var mapTiles = new ushort[MapWidth * MapHeight];
        Map map = new Map(this, mapID, mapTiles);

        maps.Add(map);
        mapTileTypes.Add(mapTiles);
        mapsById.Add(mapID, map);
    }

    public void UnloadMap(ID mapID)
    {
        
    }
}