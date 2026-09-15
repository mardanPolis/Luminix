using System;
using System.Collections.Generic;
using Luminix.Collections;
using System.Reflection;

namespace Luminix;

internal partial class EntityContainer
{
    private const int DEFAULT_ENTITY_CAPACITY = 16;

    internal EntityContainer(EntityType type, EntitySystem entitySystem, Method[] startMethods, Method[] updateMethods, Method[] renderMethods, Method[] destroyMethods, IComponentContainer[] componentContainers)
    {
        this.EntityType = type;
        this.EntitySystem = entitySystem;

        MethodInfo method = typeof(EntitySystem).GetMethod(
            "Tick", BindingFlags.NonPublic | BindingFlags.Instance);

        this.tickMethod = (Action)Delegate.CreateDelegate(typeof(Action), this.EntitySystem, method);
        this.startMethods = startMethods ?? [];
        this.updateMethods = updateMethods ?? [];
        this.renderMethods = renderMethods ?? [];
        this.destroyMethods = destroyMethods ?? [];

        this.componentContainers = componentContainers;

        this.componentTypeMap = new Dictionary<Type, int>();
        for(int i = 0; i < componentContainers.Length; i++)
        {
            var componentContainer = componentContainers[i];
            componentTypeMap.Add(componentContainer.ComponentType.Type, i);
            if(componentContainer.ComponentType.Type == typeof(EntityID))
                entityContainer = (ComponentContainer<EntityID>)componentContainer;
        }
        
        this.entityLocation = [];

        this.createdEntities = [];
        this.destroyedEntities = [];

        this.createdEntitiesBuffer = [];
        this.destroyedEntitiesBuffer = [];
    }

    public EntityType EntityType { get; }
    public EntitySystem EntitySystem { get; }

    private int entityCount;
    public int EntityCount => entityCount;

    private readonly Action tickMethod;

    private readonly Method[] startMethods;
    private readonly Method[] updateMethods;
    private readonly Method[] renderMethods;
    private readonly Method[] destroyMethods;

    private readonly DenseMap<EntityID, int> entityLocation;
    private readonly Dictionary<Type, int> componentTypeMap;
    private readonly IComponentContainer[] componentContainers;
    private readonly ComponentContainer<EntityID> entityContainer;

    private Dictionary<EntityID, int> createdEntities;
    private HashSet<EntityID> destroyedEntities;

    private int entityCountBuffer;
    private Dictionary<EntityID, int> createdEntitiesBuffer;
    private HashSet<EntityID> destroyedEntitiesBuffer;

    public bool HasComponent<T>() where T : struct
    {
        return componentTypeMap.ContainsKey(typeof(T));
    }
    public ref T GetComponent<T>(EntityID entityID) where T : struct
    {
        var type = typeof(T);

        if(!entityLocation.TryGetItem(entityID, out int entityIndex))
            throw new ArgumentException($"Entity of ID '{entityID}' does not Exist!");

        if(!componentTypeMap.TryGetValue(type, out int containerIndex))
            throw new KeyNotFoundException($"Component of type '{type.Name}' not found!");

        return ref ((T[])componentContainers[containerIndex].GetArray())[entityIndex];
    }

    public void Create(EntityID entityID)
    {
        if(entityLocation.TryAdd(entityID, entityLocation.Count))
        {
            
            entityCount++;
        }
    }

    public void Destroy(EntityID entityID)
    {
        if(!entityLocation.ContainsItem(entityID))
            return;

        if (destroyedEntities.Add(entityID))
            entityCount--;
    }

    public void Clear()
    {
        for(int i = 0; i < componentContainers.Length; i++)
            componentContainers[i].Reset(DEFAULT_ENTITY_CAPACITY);

        entityCountBuffer = 0;
        entityCount = 0;
    }

    public void Tick()
    {
        tickMethod();
        entityCountBuffer = entityCount;
        (createdEntities, createdEntitiesBuffer) = (createdEntitiesBuffer, createdEntities);
        (destroyedEntities, destroyedEntitiesBuffer) = (destroyedEntitiesBuffer, destroyedEntities);
    }

    public void DestroyEntities()
    {
        if(destroyedEntitiesBuffer.Count == 0)
            return;

        // stillborn entities - entities that got created and destroyed instantly

    }
    public void StartEntities()
    {
        
    } 
    public void UpdateEntities()
    {
        
    }
    public void RenderEntities()
    {
        
    }
}