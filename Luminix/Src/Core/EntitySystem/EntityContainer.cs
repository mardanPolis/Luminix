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
        this.Type = type;
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

        //TODO create default component values
    }

    public EntityType Type { get; }
    public EntitySystem EntitySystem { get; }

    private int entityCount;
    private int publicEntityCount;

    private readonly Action tickMethod;

    private readonly Method[] startMethods;
    private readonly Method[] updateMethods;
    private readonly Method[] renderMethods;
    private readonly Method[] destroyMethods;

    private readonly Dictionary<EntityID, int> entityLocation;
    private readonly Dictionary<Type, int> componentTypeMap;
    private readonly IComponentContainer[] componentContainers;
    private readonly ComponentContainer<EntityID> entityContainer;

    private Dictionary<EntityID, int> createdEntities;
    private HashSet<EntityID> destroyedEntities;

    private Dictionary<EntityID, int> createdEntitiesBuffer;
    private HashSet<EntityID> destroyedEntitiesBuffer;

    public int EntityCount => publicEntityCount;

    public void Create(EntityID entityID)
    {
        if(entityLocation.TryAdd(entityID, entityCount))
        {
            if(entityCount >= entityContainer.Length)
            {
                int newLength = entityContainer.Length * 2;
                for(int i = 0; i < componentContainers.Length; i++)
                    componentContainers[i].Resize(newLength);
            }
            
            for(int i = 0; i < componentContainers.Length; i++)
                componentContainers[i].Default(entityCount);
            
            entityContainer[entityCount] = entityID;
            createdEntities.Add(entityID, entityCount);

            publicEntityCount++;
            entityCount++;
        }
    }

    public void Destroy(EntityID entityID)
    {
        if(!entityLocation.ContainsKey(entityID))
            return;

        if (destroyedEntities.Add(entityID))
            publicEntityCount--;
    }

    public void Clear()
    {
        for(int i = 0; i < componentContainers.Length; i++)
            componentContainers[i].Reset(DEFAULT_ENTITY_CAPACITY);

        entityCount = 0;
        publicEntityCount = 0;
    }

    public bool HasComponent<T>() where T : struct
    {
        return componentTypeMap.ContainsKey(typeof(T));
    }

    public ref T GetComponent<T>(EntityID entityID) where T : struct
    {
        var type = typeof(T);

        if(!entityLocation.TryGetValue(entityID, out int entityIndex))
            throw new ArgumentException($"Entity of ID '{entityID}' does not Exist!");

        if(!componentTypeMap.TryGetValue(type, out int containerIndex))
            throw new KeyNotFoundException($"Component of type '{type.Name}' not found!");

        return ref ((T[])componentContainers[containerIndex].GetArray())[entityIndex];
    }

    public void Tick()
    {
        tickMethod();
        (createdEntities, createdEntitiesBuffer) = (createdEntitiesBuffer, createdEntities);
        (destroyedEntities, destroyedEntitiesBuffer) = (destroyedEntitiesBuffer, destroyedEntities);
    }

    public void DestroyEntities()
    {
        if(destroyedEntitiesBuffer.Count == 0)
            return;

        int entityCount = this.entityCount;
        int stillbornCount = 0;
        Span<int> stillbornIndices = stackalloc int[destroyedEntitiesBuffer.Count];

        foreach (EntityID destroyedEntityID in destroyedEntitiesBuffer)
        {
            entityCount--;
            int destroyedEntityIndex = entityLocation[destroyedEntityID];
            int lastEntityIndex = entityCount;

            bool isStillborn = createdEntitiesBuffer.ContainsKey(destroyedEntityID);

            entityLocation.Remove(destroyedEntityID);
            createdEntitiesBuffer.Remove(destroyedEntityID);

            int finalIndex = destroyedEntityIndex;

            if (destroyedEntityIndex != lastEntityIndex)
            {
                while (destroyedEntityIndex < lastEntityIndex)
                {
                    EntityID lastEntityID = entityContainer[lastEntityIndex];

                    if (destroyedEntitiesBuffer.Contains(lastEntityID))
                    {
                        lastEntityIndex--;
                        continue;
                    }

                    for (int i = 0; i < componentContainers.Length; i++)
                        componentContainers[i].Swap(destroyedEntityIndex, lastEntityIndex);

                    entityLocation[lastEntityID] = destroyedEntityIndex;

                    if (createdEntitiesBuffer.ContainsKey(lastEntityID))
                        createdEntitiesBuffer[lastEntityID] = destroyedEntityIndex;

                    finalIndex = lastEntityIndex;
                    break;
                }
            }

            if (isStillborn)
                stillbornIndices[stillbornCount++] = finalIndex;
        }

        int tailTop = this.entityCount - 1;
        for (int s = 0; s < stillbornCount; s++)
        {
            int from = stillbornIndices[s];
            if (from != tailTop)
            {
                for (int i = 0; i < componentContainers.Length; i++)
                    componentContainers[i].Swap(from, tailTop);
            }
            tailTop--;
        }

        for (int i = 0; i < destroyMethods.Length; i++)
            destroyMethods[i].InvokeBackFor(this.entityCount, destroyedEntitiesBuffer.Count - stillbornCount);


        this.entityCount -= this.entityCount - entityCount;

        destroyedEntitiesBuffer.Clear();
    }
    public void StartEntities()
    {
        if(createdEntitiesBuffer.Count == 0)
            return;

        for (int i = 0; i < startMethods.Length; i++)
            foreach(int index in createdEntitiesBuffer.Values)
                startMethods[i].Invoke(index);

        createdEntitiesBuffer.Clear();
    } 
    public void UpdateEntities()
    {
        if(publicEntityCount != entityCount)
        {
            publicEntityCount = entityCount;
            Logger.Get().NewLog(Logger.Log.Level.Error, $"Public entity count and internal entity count went out of sync for {Type}.EntityContainer");
        }

        for (int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].InvokeFor(entityCount);
    }
    public void RenderEntities()
    {
        for (int i = 0; i < renderMethods.Length; i++)
            renderMethods[i].InvokeFor(entityCount);
    }
}