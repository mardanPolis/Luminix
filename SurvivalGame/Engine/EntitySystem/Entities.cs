using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SurvivalGame.Engine.Rendering;

namespace SurvivalGame.Engine.EntitySystem;

public static class Entities
{
    private static readonly Dictionary<uint, DynamicEntity> dynamicEntities = [];
    private static readonly Dictionary<uint, StaticEntity> staticEntities = [];

    private static readonly Dictionary<uint, StaticEntity.Instance> staticEntitiesDatasets = [];

    private static uint dynamicEntityId = 0;
    private static uint staticEntityId = 0;

    private static Entity Create(HashSet<uint> typeEntities, bool isStatic, Func<uint, Entity.TypeValues, Entity> factory, Entity.TypeValues typeValues, Entity.Type entityType, Vector2 position, float rotation = 0f, Vector2? scale = null)
    {
        // Use provided scale or default
        Vector2 finalScale = scale ?? typeValues.StartingScale;

        Entity entity;
        uint entityId;

        if (isStatic)
        {
            // Get id
            entityId = staticEntityId++;
            
            // Create entity
            entity = factory(entityId, typeValues);

            // Add to static dictionary
            var staticEntity = entity as StaticEntity;
            staticEntities.Add(entityId, staticEntity);

            // Render data
            var instance = new StaticEntity.Instance
            {
                Position = position,
                Rotation = rotation,
                Scale = finalScale
            };
            staticEntitiesDatasets.Add(entityId, instance);
        }
        else
        {
            // Get id
            entityId = dynamicEntityId++;
            
            // Create entity
            entity = factory(entityId, typeValues);

            // Add to dynamic dictionary
            var dynamicEntity = entity as DynamicEntity;
            dynamicEntity.Position = position;
            dynamicEntity.Rotation = rotation;
            dynamicEntity.Scale = finalScale;
            dynamicEntities.Add(entityId, dynamicEntity);
        }

        typeEntities.Add(entityId);

        entity.Awake();
        entity.Start();

        return entity;
    }
    public static Entity Create(Entity.Type entityType, Vector2 position, float rotation = 0f, Vector2? scale = null)
    {
        if(EntityTypeRegistry.TryGet(entityType, out var typeEntities, out var isStatic, out var factory, out var typeValues))
            return Create(typeEntities,isStatic, factory, typeValues, entityType, position, rotation, scale);

        throw new Exception();
    }
    public static Entity Create(string registryName, Vector2 position, float rotation = 0, Vector2? scale = null)
    {
        if(EntityTypeRegistry.TryGet(registryName, out var typeEntities, out var isStatic, out var factory, out var typeValues, out var entityType))
            return Create(typeEntities, isStatic, factory, typeValues, entityType, position, rotation, scale);

        throw new Exception();
    }

    public static void Destroy(Entity entity)
    {
        Entity.Type entityType = entity.GetEntityType();
        
        if(!EntityTypeRegistry.TryGet(entityType, out var typeEntities, out var isStatic))
            throw new Exception("EntityType is not registered!");

        bool removed = typeEntities.Remove(entity.Id) && (isStatic ? staticEntities.Remove(entity.Id) : dynamicEntities.Remove(entity.Id));
        if (!removed)
        {
            Main.LOGGER.Wrn($"Unable to destroy {entity}!");
            return;
        }
        entity.OnDestroy();
        entity.isDestroyed = true;
    }

    public static bool TryGetDynamicEntity(uint entityId, out DynamicEntity _entity)
    {
        return dynamicEntities.TryGetValue(entityId, out _entity);
    }
    public static bool TryGetStaticEntity(uint entityId, out StaticEntity _entity)
    {
        return staticEntities.TryGetValue(entityId, out _entity);
    }
}