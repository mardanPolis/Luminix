using System;
using System.Collections.Generic;
using Luminix.Collections;
using Luminix.Identifiers;

namespace Luminix;

public abstract class World
{
    public World()
    {
        this.Name = GetType().Name;

        this.logger = Logger.Get();
        this.worldCameras  = [];

        this.entityIndexes = [];
        this.entities      = new Entity[128];

        this.lastEntityID = 0;
        this.entityCount  = 0;
    }
    public readonly string Name;
    public Camera MainCamera { get; set; }

    protected readonly Logger logger;
    protected readonly DenseList<Camera> worldCameras;


    private readonly Dictionary<ID, int> entityIndexes;
    private readonly Entity[] entities;

    private uint lastEntityID;
    private uint entityCount;

    internal void CreateEntity(Variant variant)
    {
        var ID = new ID(lastEntityID++);
        
    }

    internal void DestroyEntity(ID entityID)
    {
        if(!entityIndexes.TryGetValue(entityID, out int destroyedIndex))
            return;

        var lastIndex = entityCount - 1;

        if (destroyedIndex != lastIndex)
        {
            var lastEntity = entities[lastIndex];
            entities[destroyedIndex] = lastEntity;
            entityIndexes[lastEntity.ID] = destroyedIndex;
        }

        entityIndexes.Remove(entityID);
        entityCount--;
    }









    public void Tick()
    {
        for(int i = 0; i < worldCameras.Count; i++)
        {
            worldCameras[i].Tick();
        }

        for(int i = 0; i < worldComponents.Count; i++)
        {
            worldComponents[i].Tick();
        }

        OnTick();
    }

    public virtual void OnTick()
    {
        
    }
}