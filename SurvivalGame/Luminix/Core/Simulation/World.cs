using System.Collections.Generic;
using Luminix.Collections;
using Luminix.Identifiers;

namespace Luminix;

public abstract class World
{
    public World()
    {
        this.Name = GetType().Name;

        this.logger = Logger.Get();
        this.worldCameras  = [];

        this.lastEntityID = 0;
        this.entityCount  = 0;
    }
    public readonly string Name;
    public Camera MainCamera { get; set; }

    public int EntityCount => entityCount;

    protected readonly Logger logger;
    protected readonly DenseList<Camera> worldCameras;

    private Dictionary<ID, (uint entry, int index)> entityLocations;
    private TypeEntry[] typeEntries;

    private int lastEntityID;
    private int entityCount;

    public ID CreateEntity(Entity.Type type)
    {
        entityCount++;
        lastEntityID++;

        if(type.Value >= typeEntries.Length)
        {
            logger.Err($"Failed to create entity of {type}");
            return new ID(0);
        }

        var id = (ID)lastEntityID;
        var entryIndex = type.Value;
        ref var typeEntry = ref typeEntries[entryIndex];

        entityLocations.Add(id, (entryIndex, typeEntry.EntityCount));
        typeEntry.CreateEntity(id);

        return id;
    }

    public bool EntityExists(ID entityID)
    {
        return entityLocations.ContainsKey(entityID);
    }

    public bool EntityHasComponent<T>(ID entityID) where T : struct
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            throw new KeyNotFoundException($"Entity with {entityID} not found!");
        
        return typeEntries[entityLocation.entry].HasComponent(typeof(T));
    }

    public ref T GetEntityComponent<T>(ID entityID) where T : struct
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            throw new KeyNotFoundException($"Entity with {entityID} not found!");

        return ref typeEntries[entityLocation.entry].GetEntityComponent<T>(entityLocation.index);
    }

    public void DestroyEntity(ID entityID)
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            return;

        ref TypeEntry typeEntry = ref typeEntries[entityLocation.entry];
        typeEntry.DestroyEntity(entityLocation.index);
        entityLocations.Remove(entityID);
    }









    public void Tick()
    {
        for(int i = 0; i < worldCameras.Count; i++)
        {
            worldCameras[i].Tick();
        }

        OnTick();
    }

    public virtual void OnTick()
    {
        
    }
}