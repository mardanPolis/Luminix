using System;
using System.Collections.Generic;
using Luminix.Identifiers;
using Luminix.Collections;
using System.Reflection.Metadata;

namespace Luminix;

internal partial struct EntityContainer
{
    private const int DEFAULT_ENTITY_CAPACITY = 8;

    public EntityContainer(EntityType type, Method[] startMethods, Method[] updateMethods, Method[] renderMethods, Method[] destroyMethods, IStructContainer[] structContainers)
    {
        this.Type = type;
        this.EntitySystem = type.EntitySystem;

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

        this.startMethods = startMethods;
        this.updateMethods = updateMethods;
        this.renderMethods = renderMethods;
        this.destroyMethods = destroyMethods;
    }

    public EntityType Type { get; }
    public EntitySystem EntitySystem { get; }

    public int EntityCount { get; private set; }
    
    private readonly Method[] startMethods;
    private readonly Method[] updateMethods;
    private readonly Method[] renderMethods;
    private readonly Method[] destroyMethods;
    private bool dirtyMethods = false;

    private readonly Dictionary<Type, int> componentTypeMap;
    private readonly IStructContainer[] structContainers;
    private readonly StructContainer<ID> entityContainer;

    private int entityCount;

    public void CreateEntity(ID entityID)
    {
        if(EntityCount >= entityContainer.Length)
        {
            int newLength = entityContainer.Length * 2;
            for(int i = 0; i < structContainers.Length; i++)
                structContainers[i].Resize(newLength);
            
            dirtyMethods = true;
        }
        
        for(int i = 0; i < structContainers.Length; i++)
            structContainers[i].Default(EntityCount);
        
        entityContainer[EntityCount] = entityID;

        EntityCount++;
    }

    public void DestroyEntity(int entityIndex)
    {
        var lastIndex = EntityCount - 1;

        if (entityIndex != lastIndex)
        {
            for(int i = 0; i < structContainers.Length; i++)
                structContainers[i].Set(entityIndex, lastIndex);
        }
        EntityCount--;
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

        return ref ((T[])structContainers[containerIndex].GetArray())[entityIndex];
    }

    public void Refresh()
    {
        if (dirtyMethods)
            CompileMethods();
        
        entityCount = EntityCount;
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

    public void Clear()
    {
        for(int i = 0; i < structContainers.Length; i++)
            structContainers[i].Reset(DEFAULT_ENTITY_CAPACITY);

        CompileMethods();

        EntityCount = 0;
    }

    private void CompileMethods()
    {
        for(int i = 0; i < startMethods.Length; i++)
            startMethods[i].Compile();
        for(int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].Compile();
        for(int i = 0; i < renderMethods.Length; i++)
            renderMethods[i].Compile();
        for(int i = 0; i < renderMethods.Length; i++)
            destroyMethods[i].Compile();

        dirtyMethods = false;
    }
}