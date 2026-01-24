using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public static class Entities
{
    //Entity Tag Registry
    private static bool finalizedTagRegistry;
    private static readonly Dictionary<string, Entity.Tag> entityTagIds = [];
    private static string[] entityTagsStr;
    public static ushort EntityTagCount => (ushort)entityTagIds.Count;

    public static void RegisterTag(string registryTag)
    {
        registryTag = registryTag.ToLower();
        if(finalizedTagRegistry)
            throw new Exception($"Tried to register tag '{registryTag}', but Entity Tag registry already finalized!");

        if(entityTagIds.ContainsKey(registryTag))
            throw new Exception($"Entity.Tag '{registryTag}' already registered");

        if(entityTagIds.Count >= ushort.MaxValue)
            throw new Exception($"Tried to register Entity.Tag '{registryTag}', but Entity Tag registry has reached max registry count - {ushort.MaxValue}");

        entityTagIds.Add(registryTag, new Entity.Tag(EntityTagCount));
    }

    public static void FinalizeTagRegistry()
    {
        if(finalizedTagRegistry)
            throw new System.Exception($"Entities registry is already finalized!");

        finalizedTagRegistry = true;
        
        entityTagsStr = new string[EntityTagCount];
        var entityTagIdsArray = entityTagIds.ToArray();
        foreach(var entry in entityTagIdsArray)
            entityTagsStr[entry.Value.Id] = entry.Key;

        SurvivalGame.LOGGER.Log($"Registered Tags ({entityTagsStr.Length}):");
        for (int i = 0; i < entityTagsStr.Length; i++)
            SurvivalGame.LOGGER.Log($" - id:{i} -> {entityTagsStr[i]}");
    }
    
    public static bool TagExists(string registryTag)
    {
        return entityTagIds.ContainsKey(registryTag);
    }
    public static bool TagExists(Entity.Tag tag)
    {
        return tag.Id < EntityTagCount;
    }

    public static bool TryGetTag(string registryTag, out Entity.Tag tagId)
    {
        return entityTagIds.TryGetValue(registryTag, out tagId);
    }
    public static bool TryGetTag(ushort tagId, out string registryTag)
    {
        registryTag = string.Empty;

        if(entityTagsStr.Length <= tagId)
            return false;

        registryTag = entityTagsStr[tagId];
        return true;
    }



    //Entity Registry
    private struct EntityTypeData
    {
        public string registryName;
        public Type classType;
        public bool isStatic;
        public Entity.TypeValues typeValues;
        public Func<uint, Entity.TypeValues, Entity> factory;
        public List<Entity> entities;
    }

    private static bool finalizedEntityRegistry;
    private static Dictionary<string, EntityBuilder> entityBuilders = []; // Initialization only
    private static readonly Dictionary<string, Entity.Type> entityTypes = [];
    private static EntityTypeData[] entityTypeDatas;
    
    public static ushort EntityTypeCount => (ushort)entityTypes.Count;

    public static void Register(string registryName, EntityBuilder entityBuilder)
    {
        if(!finalizedTagRegistry)
            throw new Exception($"Tried to register entity '{registryName}', but Entity Tag registry isnt finalized!");

        if(finalizedEntityRegistry)
            throw new Exception($"Tried to register entity '{registryName}', but Entity registry already finalized!");

        if(entityBuilders.ContainsKey(registryName))
            throw new Exception($"Entity '{registryName}' already registered");

        if(!entityBuilder.EntityClass.IsSubclassOf(typeof(DynamicEntity)) && !entityBuilder.EntityClass.IsSubclassOf(typeof(StaticEntity)))
            throw new Exception($"Type '{entityBuilder.EntityClass.Name}' is nor a DynamicEntity, nor a StaticEntity!");

        if(entityBuilders.Count >= ushort.MaxValue)
            throw new Exception($"Tried to register '{registryName}', but Entity registry has reached max registry count - {ushort.MaxValue}");

        entityBuilder.RegistryName = registryName;
        entityBuilder.RegistryId = (ushort)entityBuilders.Count;

        entityBuilders.Add(registryName, entityBuilder);
    }

    public static bool TryGetBuilder(string registryName, out EntityBuilder builder)
    {
        if(finalizedEntityRegistry)
            throw new Exception($"Tried to get entity '{registryName}' builder, but Entity registry already finalized!");

        if (entityBuilders.TryGetValue(registryName, out var baseBuilder) && baseBuilder is EntityBuilder typedBuilder)
        {
            builder = typedBuilder;
            return true;
        }

        builder = null;
        return false;
    }

    internal static void FinalizeEntityRegistry()
    {
        if(finalizedEntityRegistry)
            throw new System.Exception($"Entities registry is already finalized!");

        finalizedEntityRegistry = true;

        entityTypeDatas = new EntityTypeData[entityBuilders.Count];

        foreach(var registry in entityBuilders)
        {
            var builder = registry.Value;
            var entityType = new Entity.Type(builder.RegistryId);
            entityTypes.Add(builder.RegistryName, entityType);
            entityTypeDatas[builder.RegistryId].registryName = builder.RegistryName;
            entityTypeDatas[builder.RegistryId].classType = builder.EntityClass;
            entityTypeDatas[builder.RegistryId].isStatic = builder.EntityClass.IsSubclassOf(typeof(StaticEntity));
            entityTypeDatas[builder.RegistryId].typeValues = builder.BuildTypeValues(entityType);
            entityTypeDatas[builder.RegistryId].factory = builder.Build();
            entityTypeDatas[builder.RegistryId].entities = [];
        }

        entityBuilders = null;

        
        SurvivalGame.LOGGER.Log($"Registered EntityTypes ({entityTypes.Count}):");
        for (int i = 0; i < entityTypeDatas.Length; i++)
            SurvivalGame.LOGGER.Log($" - id:{i} -> registryName: {entityTypeDatas[i].registryName}; class: {entityTypeDatas[i].classType.Name}; isStatic: {entityTypeDatas[i].isStatic}; typeValues: {entityTypeDatas[i].typeValues}");
    }

    public static bool TryGet(string registryName, out Entity.Type type)
    {
        return entityTypes.TryGetValue(registryName, out type);
    }
    public static bool TryGet(Entity.Type type, out string registryName)
    {
        registryName = string.Empty;

        if(entityTypes.Count <= type.Id)
            return false;

        registryName = entityTypeDatas[type.Id].registryName;
        return true;
    }
    public static bool TryGet(Entity.Type type, out Type classType)
    {
        classType = null;

        if(entityTypes.Count <= type.Id)
            return false;

        classType = entityTypeDatas[type.Id].classType;
        return true;
    }
    public static bool TryGet(Entity.Type type, out bool isStatic)
    {
        isStatic = false;

        if(entityTypes.Count <= type.Id)
            return false;

        isStatic = entityTypeDatas[type.Id].isStatic;
        return true;
    }
    public static bool TryGet(Entity.Type type, out List<Entity> entities)
    {
        entities = null;

        if(entityTypes.Count <= type.Id)
            return false;

        entities = entityTypeDatas[type.Id].entities;
        return true;
    }

    //Helper
    public static TBuilder NewBuilder<TBuilder, TEntity>() where TBuilder : EntityBuilder where TEntity : Entity
    {
        return (TBuilder)Activator.CreateInstance(typeof(TBuilder), typeof(TEntity))!;
    }



    //Runtime Instances
    private struct StaticRenderData
    {
        public Vector2 position;
        public float rotation;
        public Vector2 scale;
    }
    
    private static Dictionary<uint, DynamicEntity> dynamicEntities = [];
    private static uint dynamicEntityId = 0;
    private static Dictionary<uint, StaticEntity> staticEntities = [];
    private static uint staticEntityId = 0;
    private static Dictionary<uint, StaticRenderData> staticEntitiesDatas = [];

    public static Entity Create(Entity.Type entityType, Vector2 position, float rotation = 0f, Vector2? scale = null)
    {
        var isStatic = entityTypeDatas[entityType.Id].isStatic;
        var factory = entityTypeDatas[entityType.Id].factory;
        var typeValues = entityTypeDatas[entityType.Id].typeValues;

        // Use provided scale or default
        Vector2 finalScale = scale ?? typeValues.StartingScale;

        Entity entity;

        if (isStatic)
        {
            // Get id
            var id = staticEntityId++;
            
            // Create entity
            entity = factory(id, typeValues);

            // Add to static dictionary
            var staticEntity = entity as StaticEntity;
            staticEntities.Add(id, staticEntity);

            // Render data
            var renderData = new StaticRenderData
            {
                position = position,
                rotation = rotation,
                scale = finalScale
            };
            staticEntitiesDatas.Add(id, renderData);
        }
        else
        {
            // Get id
            var id = dynamicEntityId++;
            
            // Create entity
            entity = factory(id, typeValues);

            // Add to dynamic dictionary
            var dynamicEntity = entity as DynamicEntity;
            dynamicEntity.Position = position;
            dynamicEntity.Rotation = rotation;
            dynamicEntity.Scale = finalScale;
            dynamicEntities.Add(id, dynamicEntity);
        }

        entity.Awake();
        entity.Start();

        return entity;
    }

    public static Entity Create(string registryName, Vector2 position, float rotation = 0, Vector2? scale = null)
    {
        if(entityTypes.TryGetValue(registryName, out Entity.Type entityType))
        {
            return Create(entityType, position, rotation, scale);
        }

        return null;
    }

    public static bool TryGetDynamicEntity(uint entityId, out DynamicEntity entity)
    {
        return dynamicEntities.TryGetValue(entityId, out entity);
    }
    public static bool TryGetStaticEntity(uint entityId, out StaticEntity entity)
    {
        return staticEntities.TryGetValue(entityId, out entity);
    }
}

public class EntityBuilder
{
    public EntityBuilder(Type entityClass)
    {
        if (!typeof(Entity).IsAssignableFrom(entityClass))
            throw new Exception($"EntityBuilder cant be initialized, because the provided Type '{entityClass.Name}' must inherit from 'Entity'.");

        if (entityClass.IsAbstract)
            throw new Exception($"EntityBuilder cant be initialized, because the provided Type '{entityClass.Name}' is abstract");

        EntityClass = entityClass;
    }

    internal string RegistryName;
    internal ushort RegistryId;
    public Type EntityClass { private set; get; } = null;
    public Type TypeValuesClass { private set; get; } = typeof(Entity.TypeValues);
    public List<Entity.Tag> TypeTags { protected set; get; } = null;
    public List<Entity.Tag> Tags { protected set; get; } = null;
    public Vector2? Scale = null;

    public virtual EntityBuilder TypeValuesAs<TStaticValue>()
    {
        TypeValuesClass = typeof(TStaticValue);
        return this;
    }
    public virtual EntityBuilder Tagged(string tag, bool isTypeTag)
    {
        if(isTypeTag && TypeValuesClass == null)
            throw new Exception($"Unable to Type Tag, because the TypeValues class is not set.");

        if(Entities.TryGetTag(tag, out Entity.Tag tagId))
        {
            if(isTypeTag)
            {
                TypeTags ??= [];
                TypeTags.Add(tagId);
            }
            else
            {
                Tags ??= [];
                Tags.Add(tagId);
            }
            return this;
        }

        throw new Exception($"Tag {tag} does not exist!");
    }
    public virtual EntityBuilder UnTagged(string tag)
    {
        if(Entities.TryGetTag(tag, out Entity.Tag tagId))
        {
            Tags?.Remove(tagId);
            TypeTags?.Remove(tagId);
        }
        
        return this;
    }
    public virtual EntityBuilder ClearTags(bool staticTags)
    {   
        if(!staticTags)
            Tags = null;
        else
            TypeTags = null;
            
        return this;
    }

    public virtual Func<uint, Entity.TypeValues, Entity> Build()
    {
        var ci = EntityClass.GetConstructor(new[] { typeof(uint), typeof(Entity.TypeValues) }) ?? throw new Exception($"{EntityClass.Name} must have ctor (uint, Entity.TypeValues)");
        var id = Expression.Parameter(typeof(uint));
        var tv = Expression.Parameter(typeof(Entity.TypeValues));
        Func<uint, Entity.TypeValues, Entity> factory = Expression.Lambda<Func<uint, Entity.TypeValues, Entity>>(
            Expression.New(ci, id, tv), id, tv
        ).Compile();

        Func<uint, Entity.TypeValues, Entity> result = factory;

        if (Tags != null)
        {
            var tags = Tags.ToArray();
            var prev = result;
            result = (id, tv) =>
            {
                var e = prev(id, tv);
                foreach (var t in tags) e.AddTag(t);
                return e;
            };
        }

        return result;
    }
    public virtual Entity.TypeValues BuildTypeValues(Entity.Type entityType)
    {
        Entity.TypeValues typeValues = (Entity.TypeValues)Activator.CreateInstance(TypeValuesClass, entityType);
        if(TypeTags != null)
            typeValues.SetTags(TypeTags);
        if(Scale != null)
            typeValues.SetStartingScale(Scale.Value);
        return typeValues;
    }
}