using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Luminix;

public sealed partial class Universe : Workspace.Service
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

        public readonly void StartCreatedEntities()
        {
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].StartEntities();
            }
        }

        public readonly void UpdateEntities()
        {
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].UpdateEntities();
            }
        }

        public readonly void RenderEntities()
        {
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].RenderEntities();
            }
        }

        public readonly void DestroyEntities()
        {
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].DestroyEntities();
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
    public uint TickRate { get; set; }
    public long TickCount { get; private set; }
    public double TickTime { get; private set; }
    public double TickDeltaTime { get; private set; }
    public long TickAllocated { get; private set; }

    private EntityID entityID;

    private Dictionary<EntityID, EntityType> entities;

    private Dictionary<EntityType, EntityContainer> typeContainers;
    private EntityContainer[] entityContainers;

    protected override void Start()
    {
        var defs = Workspace.Current.GetService<Defs>();
        var entityTypes = defs.GetDefs<EntityType>();
        entityContainers = CreateEntityContainers(entityTypes);
        entities = [];
        
        foreach(EntityContainer container in entityContainers)
            typeContainers[container.Type] = container;

        foreach(EntityContainer container in entityContainers)
            container.EntitySystem.Initialize(container.Type);
    }

    public EntityID CreateEntity(EntityType entityType)
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
        {
            throw new ArgumentException("Unknown entity type", nameof(entityType));
        }

        entityID = new EntityID(entityID.Value + 1);
        container.Create(entityID);
        entities[entityID] = entityType; 

        return entityID;
    }
    public bool EntityExists(EntityID entityID)
    {
        return entities.ContainsKey(entityID);
    }
    public bool DestroyEntity(EntityID entityID)
    {
        if (!entities.TryGetValue(entityID, out EntityType entityType))
        {
            return false;
        }

        if (!typeContainers.TryGetValue(entityType, out EntityContainer container))
        {
            return false;
        }

        container.Destroy(entityID);
        entities.Remove(entityID);
        return true;
    }

    public ref T GetEntityComponent<T>(EntityID entityID) where T : struct
    {
        if (!entities.TryGetValue(entityID, out EntityType entityType))
        {
            throw new ArgumentException($"Entity does not exist", nameof(entityID));
        }

        return ref typeContainers[entityType].GetComponent<T>(entityID);
    }
    public EntityType GetEntityType(EntityID entityID)
    {
        if (!entities.TryGetValue(entityID, out EntityType entityType))
        {
            throw new ArgumentException($"Entity does not exist", nameof(entityID));
        }

        return entityType;
    }
    
    public bool HasComponent<T>(EntityType entityType) where T : struct
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
        {
            throw new ArgumentException("Unknown entity type", nameof(entityType));
        }

        return container.HasComponent<T>();
    }
    
    public EntitySystem GetEntitySystem(EntityType entityType)
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
        {
            throw new ArgumentException("Unknown entity type", nameof(entityType));
        }

        return container.EntitySystem;
    }
    public int GetEntityCount(EntityType entityType)
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
        {
            throw new ArgumentException("Unknown entity type", nameof(entityType));
        }

        return container.EntityCount;
    }
}