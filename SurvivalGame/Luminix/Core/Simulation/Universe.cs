using System;
using System.Collections.Generic;
using Luminix.Collections;
using Luminix.Identifiers;

namespace Luminix;

public sealed class Universe : Workspace.Service
{
    internal readonly struct Controller 
    {
        internal Controller(Universe controlled)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a Universe.Controller, the provided universe is already initialized!");
            
            this.controlled = controlled;
        }

        private readonly Universe controlled;

        public uint TickRate { get => controlled.TickRate; set => controlled.TickRate = value; }
        public long TickCount { get => controlled.TickCount; set => controlled.TickCount = value; }
        public double TickTime { get => controlled.TickTime; set => controlled.TickTime = value; }
        public double TickDeltaTime { get => controlled.TickDeltaTime; set => controlled.TickDeltaTime = value; }
        public long TickAllocated { get => controlled.TickAllocated; set => controlled.TickAllocated = value; }

        public void Tick()
        {
            var entityTypes = controlled.entityContainers;
            var destroyedEntities = controlled.destroyedEntities;
            var entityLocations = controlled.entityLocations;

            for(int d = 0; d < destroyedEntities.Count; d++)
            {
                var destroyedEntity = destroyedEntities[d];
                ref EntityContainer entityContainer = ref entityTypes[destroyedEntity.entry];
                entityContainer.DestroyEntity(destroyedEntity.index);
                entityLocations.Remove(destroyedEntity.id);
            }

            destroyedEntities.Clear();

            for(int t = 0; t < entityTypes.Length; t++)
            {
                entityTypes[t].Refresh();
            }

            var cameras = controlled.worldCameras;

            for(int i = 0; i < cameras.Count; i++)
            {
                cameras[i].Tick();
            }

            for(int i = 0; i < entityTypes.Length; i++)
            {
                entityTypes[i].UpdateEntities();
            }
        }
    }

    internal Universe(out Controller controller)
    {
        controller = new Controller(this);
        this.controllerIssued = true;
    }

    private readonly bool controllerIssued;

    //Global Variables
    public uint TickRate { get; private set; }
    public long TickCount { get; private set; }
    public double TickTime { get; private set; }
    public double TickDeltaTime { get; private set; }
    public long TickAllocated { get; private set; }

    public int EntityCount => entityCount;

    public Camera MainCamera { get; set; }

    private readonly DenseList<Camera> worldCameras;

    private EntityTypeRegistry typeRegistry;
    private EntityContainer[] entityContainers;

    private readonly Dictionary<ID, (uint entry, int index)> entityLocations;
    private readonly DenseList<(ID id, uint entry, int index)> destroyedEntities;

    private int lastEntityID;
    private int entityCount;

    protected override void Start()
    {
        this.lastEntityID = 0;
        this.entityCount  = 0;

        this.typeRegistry = Workspace.Current.GetService<EntityTypeRegistry>();
    }

    public ID CreateEntity(string typeName)
    {
        entityCount++;
        lastEntityID++;

        if(type.ID >= entityContainers.Length)
        {
            logger.Err($"Failed to create entity of {type}");
            return new ID(0);
        }

        var id = (ID)lastEntityID;
        var entryIndex = type.ID;
        ref var typeEntry = ref entityContainers[entryIndex];

        entityLocations.Add(id, (entryIndex, typeEntry.EntityCount));
        typeEntry.CreateEntity(id);

        return id;
    }

    public void DestroyEntity(ID entityID)
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            return;
        
        destroyedEntities.Add((entityID, entityLocation.entry, entityLocation.index));
    }

    public bool EntityExists(ID entityID)
    {
        return entityLocations.ContainsKey(entityID);
    }

    public bool EntityHasComponent<T>(ID entityID) where T : struct
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            throw new KeyNotFoundException($"Entity with {entityID} not found!");
        
        return entityContainers[entityLocation.entry].HasComponent(typeof(T));
    }
    public ref T GetEntityComponent<T>(ID entityID) where T : struct
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            throw new KeyNotFoundException($"Entity with {entityID} not found!");

        return ref entityContainers[entityLocation.entry].GetEntityComponent<T>(entityLocation.index);
    }

}