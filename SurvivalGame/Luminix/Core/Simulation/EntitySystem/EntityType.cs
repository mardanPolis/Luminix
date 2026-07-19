using System;
using System.Collections.Generic;
using Luminix.Identifiers;

namespace Luminix;

internal partial struct EntityType
{
    public EntityType(string typeName, Entity.TypeID typeID, EntitySystem entitySystem, Method[] updateMethods, Method renderMethod, IComponentContainer[] componentContainers)
    {
        int defaultEntityCount = 8;

        this.TypeName = typeName;
        this.TypeID = typeID;
        this.entities = new Entity[defaultEntityCount];
        this.EntitySystem = entitySystem;
        this.updateMethods = updateMethods;

        this.componentTypeMap = new Dictionary<Type, int>();
        this.componentContainers = componentContainers;
        for(int i = 0; i < componentContainers.Length; i++)
        {
            var container = componentContainers[i];
            var updateMethod = updateMethods[i];
            componentTypeMap.Add(container.ComponentType, i);
            container.Reset(defaultEntityCount);
        }

        for(int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].Compile(this.entities);
    }

    public string TypeName { get; }
    public Entity.TypeID TypeID { get; }
    public EntitySystem EntitySystem { get; }

    public Entity[] Entities => entities;
    public int EntityCount { get; private set; }

    private readonly Dictionary<Type, int> componentTypeMap;
    private readonly IComponentContainer[] componentContainers;
    private readonly Method[] updateMethods;
    private readonly Method renderMethod;
    private bool dirtyUpdateMethods = false;

    private Entity[] entities;
    private int currEntityCount;

    public void CreateEntity(ID entityID)
    {
        if(EntityCount >= entities.Length)
        {
            int newLength = entities.Length * 2;
            Array.Resize(ref entities, newLength);
            for(int i = 0; i < componentContainers.Length; i++)
                componentContainers[i].Resize(newLength);
            
            dirtyUpdateMethods = true;
        }
        
        entities[EntityCount] = new Entity(entityID);
        for(int i = 0; i < componentContainers.Length; i++)
            componentContainers[i].Default(EntityCount);
        
        EntityCount++;
    }

    public bool HasComponent(Type componentType)
    {
        return componentTypeMap.ContainsKey(componentType);
    }

    public ref T GetEntityComponent<T>(int entityIndex) where T : struct
    {
        var type = typeof(T);

        if(!componentTypeMap.TryGetValue(type, out int containerIndex))
            throw new KeyNotFoundException($"Component of type '{type.Name}' not found!");

        return ref ((T[])componentContainers[containerIndex].GetArray())[entityIndex];
    }

    public void DestroyEntity(int entityIndex)
    {
        var lastIndex = EntityCount - 1;

        if (entityIndex != lastIndex)
        {
            var lastEntity = entities[lastIndex];
            entities[entityIndex] = lastEntity;
            for(int i = 0; i < componentContainers.Length; i++)
                componentContainers[i].Set(entityIndex, lastIndex);
        }
        EntityCount--;
    }

    public void Refresh()
    {
        if (dirtyUpdateMethods)
        {
            for (int i = 0; i < updateMethods.Length; i++)
                updateMethods[i].Compile(entities);

            renderMethod.Compile(entities);

            dirtyUpdateMethods = false;
        }
        
        currEntityCount = EntityCount;
    }

    public void UpdateEntities()
    {
        var entityCount = currEntityCount;

        for (int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].Invoke(entityCount);
    }
}