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

        this.destroyedEntities = [];

        this.lastEntityID = 0;
        this.entityCount  = 0;

        this.universe = Workspace.Current.GetService<Universe>();
        this.typeRegistry = Workspace.Current.GetService<EntityTypeRegistry>();
        this.entityTypes = typeRegistry.GetEntityTypes();
        
    }
    protected Universe universe;
    protected EntityTypeRegistry typeRegistry;


    public readonly string Name;
    public Camera MainCamera { get; set; }

    public int EntityCount => entityCount;

    protected readonly Logger logger;
    protected readonly DenseList<Camera> worldCameras;

    private Dictionary<ID, (uint entry, int index)> entityLocations;
    private EntityType[] entityTypes;

    private DenseList<(ID id, uint entry, int index)> destroyedEntities;

    private int lastEntityID;
    private int entityCount;

    public ID CreateEntity(Entity.TypeID type)
    {
        entityCount++;
        lastEntityID++;

        if(type.Value >= entityTypes.Length)
        {
            logger.Err($"Failed to create entity of {type}");
            return new ID(0);
        }

        var id = (ID)lastEntityID;
        var entryIndex = type.Value;
        ref var typeEntry = ref entityTypes[entryIndex];

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
        
        return entityTypes[entityLocation.entry].HasComponent(typeof(T));
    }

    public ref T GetEntityComponent<T>(ID entityID) where T : struct
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            throw new KeyNotFoundException($"Entity with {entityID} not found!");

        return ref entityTypes[entityLocation.entry].GetEntityComponent<T>(entityLocation.index);
    }

    public void DestroyEntity(ID entityID)
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            return;
        
        destroyedEntities.Add((entityID, entityLocation.entry, entityLocation.index));
    }

    internal void CreateEntities()
    {
        
    }
    internal void DestroyEntities()
    {
        for(int i = 0; i < destroyedEntities.Count; i++)
        {
            var destroyedEntity = destroyedEntities[i];
            ref EntityType typeEntry = ref entityTypes[destroyedEntity.entry];
            typeEntry.DestroyEntity(destroyedEntity.index);
            entityLocations.Remove(destroyedEntity.id);
        }

        destroyedEntities.Clear();
    }


    public void Tick()
    {
        for(int i = 0; i < worldCameras.Count; i++)
        {
            worldCameras[i].Tick();
        }

        OnTick();

        for(int i = 0; i < entityTypes.Length; i++)
        {
            entityTypes[i].UpdateEntities();
        }

        DestroyEntities();
    }

    public virtual void OnTick()
    {
        
    }
}