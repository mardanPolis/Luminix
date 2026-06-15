using System;
using System.Collections.Generic;
using Luminix.Identifiers;

namespace Luminix;

internal struct TypeEntry
{
    public TypeEntry(string name, Entity.Type entityType, EntitySystem entitySystem, Type[] componentTypes)
    {
        int defaultEntityCount = 8;

        Name = name;
        EntityType = entityType;
        entities = new Entity[defaultEntityCount];
        EntitySystem = entitySystem;

        componentTypeMap = new Dictionary<Type, int>();
        componentContainers = new IComponentContainer[componentTypes.Length];
        for(int i = 0; i < componentTypes.Length; i++)
        {
            Type componentType = componentTypes[i];
            componentTypeMap.Add(componentType, i);
        }

        //TODO : create ComponentContainers
    }

    public string Name { get; }
    public Entity.Type EntityType { get; }
    public EntitySystem EntitySystem { get; }

    public Entity[] Entities => entities;
    public int EntityCount { get; private set; }

    private Entity[] entities;
    private Dictionary<Type, int> componentTypeMap { get; }
    private IComponentContainer[] componentContainers { get; }

    public void CreateEntity(ID entityID)
    {
        if(EntityCount >= entities.Length)
        {
            int newLength = entities.Length * 2;
            Array.Resize(ref entities, newLength);
            for(int i = 0; i < componentContainers.Length; i++)
                componentContainers[i].Resize(newLength);
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
}