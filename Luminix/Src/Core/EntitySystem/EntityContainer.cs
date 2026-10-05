using System;
using System.Collections.Generic;
using Luminix.Collections;
using System.Reflection;
using System.Collections;

namespace Luminix;

internal partial class EntityContainer
{
    private const int DEFAULT_ENTITY_CAPACITY = 16;

    internal EntityContainer(EntityType type, EntitySystem entitySystem, Method[] startMethods, Method[] prepareMethods, Method[] updateMethods, Method[] renderMethods, Method[] destroyMethods, IComponentContainer[] componentContainers)
    {
        this.EntityType = type;
        this.EntitySystem = entitySystem;

        MethodInfo method = typeof(EntitySystem).GetMethod(
            "Tick", BindingFlags.NonPublic | BindingFlags.Instance);

        this.tickMethod = (Action)Delegate.CreateDelegate(typeof(Action), this.EntitySystem, method);
        this.startMethods = startMethods ?? [];
        this.prepareMethods = prepareMethods ?? [];
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
        
        if(entityContainer == null)
            throw new ArgumentException("Expected ComponentContainer<EntityID>, but never received one!");

        this.entityLocations = [];

        this.createdEntities = [];
        this.destroyedEntities = [];

        this.createdEntitiesBuffer = [];
        this.destroyedEntitiesBuffer = [];
    }

    public EntityType EntityType { get; }
    public EntitySystem EntitySystem { get; }

    private readonly Action tickMethod;

    private readonly Method[] startMethods;
    private readonly Method[] prepareMethods;
    private readonly Method[] updateMethods;
    private readonly Method[] renderMethods;
    private readonly Method[] destroyMethods;

    private int entityCount;
    private int entityCountInternal;
    public int EntityCount => entityCount;

    private readonly DenseMap<EntityID, int> entityLocations;
    private readonly Dictionary<Type, int> componentTypeMap;
    private readonly IComponentContainer[] componentContainers;
    private readonly ComponentContainer<EntityID> entityContainer;

    private HashSet<EntityID> createdEntities;
    private HashSet<EntityID> destroyedEntities;

    private HashSet<EntityID> createdEntitiesBuffer;
    private HashSet<EntityID> destroyedEntitiesBuffer;

    public bool HasComponent<T>() where T : struct
    {
        return componentTypeMap.ContainsKey(typeof(T));
    }
    public ref T GetComponent<T>(EntityID entityID) where T : struct
    {
        var type = typeof(T);

        if(!entityLocations.TryGetItem(entityID, out int entityIndex))
            throw new ArgumentException($"Entity of ID '{entityID}' does not Exist!");

        if(!componentTypeMap.TryGetValue(type, out int containerIndex))
            throw new KeyNotFoundException($"Component of type '{type.Name}' not found!");

        return ref ((T[])componentContainers[containerIndex].GetArray())[entityIndex];
    }

    public void Clear()
    {
        for(int i = 0; i < componentContainers.Length; i++)
            componentContainers[i].Reset(DEFAULT_ENTITY_CAPACITY);

        createdEntities.Clear();
        destroyedEntities.Clear();
        createdEntitiesBuffer.Clear();
        destroyedEntitiesBuffer.Clear();

        entityCount = 0;
    }

    public void Allocate(EntityID entityID)
    {
        var index = entityLocations.Count;
        if(entityLocations.TryAdd(entityID, index) && createdEntities.Add(entityID))
        {
            if(index >= entityContainer.Capacity)
            {
                int newLength = entityContainer.Capacity * 2;
                for(int i = 0; i < componentContainers.Length; i++)
                    componentContainers[i].Resize(newLength);
            }
            
            for(int i = 0; i < componentContainers.Length; i++)
                componentContainers[i].Default(index);
            
            entityContainer[index] = entityID;
            createdEntities.Add(entityID);

            entityCount++;
        }
    }

    public void Destroy(EntityID entityID)
    {
        if(!entityLocations.ContainsItem(entityID))
            return;

        if (destroyedEntities.Add(entityID))
            entityCount--;
    }

    public void Tick()
    {
        tickMethod();
        ReorganizeEntities();
        entityCountInternal = entityLocations.Count;
        createdEntitiesBuffer.Clear();
        destroyedEntitiesBuffer.Clear();
        (createdEntities, createdEntitiesBuffer) = (createdEntitiesBuffer, createdEntities);
        (destroyedEntities, destroyedEntitiesBuffer) = (destroyedEntitiesBuffer, destroyedEntities);

    }

    private void ReorganizeEntities()
    {
        if(destroyedEntitiesBuffer.Count == 0)
            return;

        if(createdEntities.Count == 0)
            return;
        
        var firstIndex = entityCountInternal;

        foreach(var entity in createdEntities)
        {
            var entityIndex = entityLocations[entity];
            if(entityIndex < firstIndex)
                continue;

            while (firstIndex < entityIndex && 
                entityLocations.ContainsItem(firstIndex))// since I removed the index in destroy step it does not exist at this time in the map.
            {
                firstIndex++;
            }

            if (firstIndex >= entityIndex)
                continue;

            for (int i = 0; i < componentContainers.Length; i++)
                componentContainers[i].Swap(entityIndex, firstIndex);
            
            entityLocations[entity] = firstIndex;
            firstIndex++;
        }
    }

    public void DestroyEntities()
    {
        if(destroyedEntitiesBuffer.Count == 0)
            return;

        var lastIndex = entityCountInternal - 1;
        int stillbornCount = 0;
        Span<int> stillbornIndices = stackalloc int[destroyedEntitiesBuffer.Count];

        foreach (var entity in destroyedEntitiesBuffer)
        {
            int entityIndex = entityLocations[entity];

            bool stillborn = createdEntitiesBuffer.Contains(entity); // - entities that got created and instantly got destroyed

            entityLocations.Remove(entity, entityIndex);
            createdEntitiesBuffer.Remove(entity);

            while (entityIndex < lastIndex)
            {
                var lastEntity = entityContainer[lastIndex];
                if(destroyedEntitiesBuffer.Contains(lastEntity))
                {
                    lastIndex--;
                    continue;
                }

                for (int i = 0; i < componentContainers.Length; i++)
                    componentContainers[i].Swap(entityIndex, lastIndex);

                entityLocations[lastEntity] = entityIndex;
                lastIndex--;
                break;
            }

            if (stillborn)
                stillbornIndices[stillbornCount++] = lastIndex;

        }

        int tailTop = this.entityCountInternal - 1; // sorts [(entities) (destroyed) (stillborn)]
        for (int sbIndex = 0; sbIndex < stillbornCount; sbIndex++)
        {
            int from = stillbornIndices[sbIndex];
            if (from != tailTop)
            {
                for (int i = 0; i < componentContainers.Length; i++)
                    componentContainers[i].Swap(from, tailTop);
            }
            tailTop--;
        }

        for (int i = 0; i < destroyMethods.Length; i++)
            destroyMethods[i].InvokeBackFor(this.entityCountInternal - stillbornCount, destroyedEntitiesBuffer.Count - stillbornCount);

        entityCountInternal -= destroyedEntitiesBuffer.Count;
    }
    public void StartEntities()
    {
        if(createdEntitiesBuffer.Count == 0)
            return;

        foreach (var entity in createdEntitiesBuffer)
        {
            int entityIndex = entityLocations[entity];

            for(int i = 0; i < startMethods.Length; i++)
                startMethods[i].Invoke(entityIndex);
        }
    }
    public void PrepareEntities()
    {
        for (int i = 0; i < prepareMethods.Length; i++)
            prepareMethods[i].InvokeFor(entityCountInternal);
    }
    public void UpdateEntities()
    {
        for (int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].InvokeFor(entityCountInternal);
    }
    public void RenderEntities()
    {
        for (int i = 0; i < renderMethods.Length; i++)
            renderMethods[i].InvokeFor(entityCountInternal);
    }

    public DenseMap<EntityID, int>.LeftEnumerable.Enumerator GetEntityEnumerator() => entityLocations.LeftItems.GetEnumerator();
    
}