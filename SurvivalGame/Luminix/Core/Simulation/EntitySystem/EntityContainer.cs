using System;
using System.Collections.Generic;
using Luminix.Identifiers;
using Luminix.Collections;
using System.Reflection;

namespace Luminix;

internal partial class EntityContainer
{
    private const int DEFAULT_ENTITY_CAPACITY = 16;

    public EntityContainer(EntityType type)
    {
        this.Type = type;
        this.EntitySystem = Type.EntitySystem;
        
        MethodInfo method = typeof(EntitySystem).GetMethod(
            "Tick", BindingFlags.NonPublic | BindingFlags.Instance);
        tickMethod = (Action)Delegate.CreateDelegate(typeof(Action), this.EntitySystem, method);

        this.startMethods = startMethods;
        this.updateMethods = updateMethods;
        this.renderMethods = renderMethods;
        this.destroyMethods = destroyMethods;

        this.destroyedEntities = new HashSet<ID>();
        this.entityLocation = new Dictionary<ID, int>();

        this.componentTypeMap = new Dictionary<Type, int>();
        for(int i = 0; i < structContainers.Length; i++)
        {
            var structContainer = structContainers[i];
            componentTypeMap.Add(structContainer.StructType, i);
            if(structContainer.StructType == typeof(ID))
                entityContainer = (StructContainer<ID>)structContainer;
        }

        this.structContainers = structContainers;
        
        Clear();
    }

    public EntityType Type { get; }
    public EntitySystem EntitySystem { get; }

    private int entityCount;
    
    private readonly Action tickMethod;

    private readonly Method[] startMethods;
    private readonly Method[] updateMethods;
    private readonly Method[] renderMethods;
    private readonly Method[] destroyMethods;

    private readonly Dictionary<ID, int> entityLocation;
    private readonly Dictionary<Type, int> componentTypeMap;
    private readonly IStructContainer[] structContainers;
    private readonly StructContainer<ID> entityContainer;

    private readonly Dictionary<ID, int> createdEntities;
    private readonly HashSet<ID> destroyedEntities;

    public void Create(ID entityID)
    {
        if(entityCount >= entityContainer.Length)
        {
            int newLength = entityContainer.Length * 2;
            for(int i = 0; i < structContainers.Length; i++)
                structContainers[i].Resize(newLength);
        }
        
        for(int i = 0; i < structContainers.Length; i++)
            structContainers[i].Default(entityCount);
        
        entityContainer[entityCount] = entityID;
        entityLocation.Add(entityID, entityCount);
        createdEntities.Add(entityID, entityCount);
    }

    public void Destroy(ID entityID)
    {
        destroyedEntities.Add(entityID);
    }

    public void Clear()
    {
        for(int i = 0; i < structContainers.Length; i++)
            structContainers[i].Reset(DEFAULT_ENTITY_CAPACITY);

        entityCount = 0;
    }



    public bool HasComponent<T>() where T : struct
    {
        return componentTypeMap.ContainsKey(typeof(T));
    }

    public ref T GetComponent<T>(int entityIndex) where T : struct
    {
        var type = typeof(T);

        if(!componentTypeMap.TryGetValue(type, out int containerIndex))
            throw new KeyNotFoundException($"Component of type '{type.Name}' not found!");

        return ref ((T[])structContainers[containerIndex].GetArray())[entityIndex];
    }



    public void TickSystems()
    {
        tickMethod();
    }

    public void StartEntities()
    {
        if(createdEntities.Count == 0)
            return;

        for (int i = 0; i < startMethods.Length; i++)
            startMethods[i].Invoke(createdEntities.Values);

        entityCount += createdEntities.Count;
        createdEntities.Clear();
    } 
    public void UpdateEntities()
    {
        for (int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].InvokeFor(entityCount);
    }

    public void RenderEntities()
    {
        for (int i = 0; i < renderMethods.Length; i++)
            renderMethods[i].InvokeFor(entityCount);
    }
    public void DestroyEntities()
    {
        int count = destroyedEntities.Count;

        if(count == 0)
            return;

        int entityCount = this.entityCount;

        foreach(ID entity in destroyedEntities)
        {
            entityCount--;
            var lastIndex = entityCount;
            var entityIndex = entityLocation[entity];
            
            entityLocation.Remove(entity);

            //if destroyed entity is last
            if (entityIndex == lastIndex)
                continue;

            ID otherEntity = entityContainer[lastIndex];
            for(int i = 0; i < structContainers.Length; i++)
                structContainers[i].Swap(entityIndex, lastIndex);

            entityLocation[otherEntity] = entityIndex;

            if(createdEntities.Count == 0 || !createdEntities.ContainsKey(otherEntity))
                continue;

            createdEntities[otherEntity] = entityIndex;
        }

        for (int i = 0; i < destroyMethods.Length; i++)
            destroyMethods[i].InvokeBackFor(this.entityCount, count);

        this.entityCount = entityCount;

        destroyedEntities.Clear();
    }
}