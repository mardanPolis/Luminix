using System;
using System.Collections.Generic;
using Game;
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

        internal void RefreshData()
        {   
            var entityTypes = controlled.entityTypes;
            var destroyedEntities = controlled.destroyedEntities;
            var entityLocations = controlled.entityLocations;

            for(int d = 0; d < destroyedEntities.Count; d++)
            {
                var destroyedEntity = destroyedEntities[d];
                ref EntityType typeEntry = ref entityTypes[destroyedEntity.entry];
                typeEntry.DestroyEntity(destroyedEntity.index);
                entityLocations.Remove(destroyedEntity.id);
            }

            destroyedEntities.Clear();

            for(int t = 0; t < entityTypes.Length; t++)
            {
                entityTypes[t].Refresh();
            }
        }


        public void Tick()
        {
            var cameras = controlled.worldCameras;
            var entityTypes = controlled.entityTypes;

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

    protected override void Start()
    {
        this.lastEntityID = 0;
        this.entityCount  = 0;

        this.typeRegistry = Workspace.Current.GetService<EntityTypeRegistry>();
        this.entityTypes = typeRegistry.GetEntityTypes();
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
    private EntityType[] entityTypes;

    private readonly Dictionary<ID, (uint entry, int index)> entityLocations;
    private readonly DenseList<(ID id, uint entry, int index)> destroyedEntities;

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
        
        return entityTypes[entityLocation.entry].HasComponent(typeof(T));
    }
    public ref T GetEntityComponent<T>(ID entityID) where T : struct
    {
        if(!entityLocations.TryGetValue(entityID, out var entityLocation))
            throw new KeyNotFoundException($"Entity with {entityID} not found!");

        return ref entityTypes[entityLocation.entry].GetEntityComponent<T>(entityLocation.index);
    }

}