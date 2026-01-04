using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public static class Entities
{
    private static Dictionary<string, ushort> entityTagIds = new();
    private static string[] entityTags;
    private static List<EntityBuilder> entityBuilders = new();
    private static Dictionary<string, EntityType> entityTypes = new();
    private static EntityTypeData[] entityTypeDatas;
    
    public static ushort EntityTagCount => (ushort)entityTagIds.Count;
    public static ushort EntityTypeCount => (ushort)entityTypes.Count;

    private static bool finalized;

    public static void RegisterTag(string registryTag)
    {
        registryTag = registryTag.ToLower();
        if(finalized)
            throw new Exception($"Tried to register tag '{registryTag}', but Entities registry already finalized!");

        if(entityTagIds.ContainsKey(registryTag)) // For MODDING SUPPORT MUST REMOVE THE ENTITYTYPE CHECK, INSTEAD I SHOULD USE ENTITYBUILDERS AND BUILD WHEN FINALIZING, IF A BUILDER EXISTS FOR THAT REGISTRY NAME REPLACE PARTS.
            throw new Exception($"EntityTag '{registryTag}' already registered");

        if(entityTagIds.Count >= ushort.MaxValue)
            throw new Exception($"Tried to register tag '{registryTag}', but Entity registry has reached max registry count - {ushort.MaxValue}");

        entityTagIds.Add(registryTag, EntityTagCount);
    }
    public static void Register<T>(string registryName, EntityBuilder<T> entityBuilder) where T : Entity
    {
        if(finalized)
            throw new Exception($"Tried to register entity '{registryName}', but Entities registry already finalized!");

        if(entityTypes.ContainsKey(registryName)) // For MODDING SUPPORT MUST REMOVE THE ENTITYTYPE CHECK, INSTEAD I SHOULD USE ENTITYBUILDERS AND BUILD WHEN FINALIZING, IF A BUILDER EXISTS FOR THAT REGISTRY NAME REPLACE PARTS.
            throw new Exception($"Entity '{registryName}' already registered");

        if(!typeof(T).IsSubclassOf(typeof(DynamicEntity)) && !typeof(T).IsSubclassOf(typeof(StaticEntity)))
            throw new Exception($"Type '{typeof(T).Name}' is not a DynamicEntity nor a StaticEntity!");

        if(entityTypes.Count >= ushort.MaxValue)
            throw new Exception($"Tried to register '{registryName}', but Entity registry has reached max registry count - {ushort.MaxValue}");

        entityBuilder.registryName = registryName;
        entityBuilder.registryId = (ushort)entityBuilders.Count;

        entityBuilders.Add(entityBuilder);
    }

    internal static void FinalizeRegistries()
    {
        if(finalized)
            throw new System.Exception($"Entities registry is already finalized!");

        finalized = true;

        //tags
        entityTags = new string[EntityTagCount];
        var entityTagIdsArray = entityTagIds.ToArray();
        foreach(var entry in entityTagIdsArray)
            entityTags[entry.Value] = entry.Key;

        //entities
        entityTypeDatas = new EntityTypeData[entityBuilders.Count];

        foreach(var builder in entityBuilders)
        {
            entityTypes.Add(builder.registryName, new EntityType(builder.registryId));
            entityTypeDatas[builder.registryId].registryName = builder.registryName;
            entityTypeDatas[builder.registryId].classType = builder.entityClassType;
            entityTypeDatas[builder.registryId].isStatic = builder.entityClassType.IsSubclassOf(typeof(StaticEntity));
            entityTypeDatas[builder.registryId].factory = builder.BuildDelegate() as Func<Entity>;
            entityTypeDatas[builder.registryId].entities = new();
        }

        entityBuilders = null;
    }

    public static bool TagExists(string registryTag)
    {
        return entityTagIds.ContainsKey(registryTag);
    }

    public static bool TryGetTag(string registryTag, out ushort tagId)
    {
        return entityTagIds.TryGetValue(registryTag, out tagId);
    }
    public static bool TryGetTag(ushort tagId, out string registryTag)
    {
        registryTag = string.Empty;

        if(entityTags.Length <= tagId)
            return false;

        registryTag = entityTags[tagId];
        return true;
    }

    public static bool TryGet(string registryName, out EntityType type)
    {
        return entityTypes.TryGetValue(registryName, out type);
    }
    public static bool TryGet(EntityType type, out string registryName)
    {
        registryName = string.Empty;

        if(entityTypes.Count <= type.Id)
            return false;

        registryName = entityTypeDatas[type.Id].registryName;
        return true;
    }
    public static bool TryGet(EntityType type, out bool isStatic)
    {
        isStatic = false;

        if(entityTypes.Count <= type.Id)
            return false;

        isStatic = entityTypeDatas[type.Id].isStatic;
        return true;
    }
    public static bool TryGet(EntityType type, out List<Entity> entities)
    {
        entities = null;

        if(entityTypes.Count <= type.Id)
            return false;

        entities = entityTypeDatas[type.Id].entities;
        return true;
    }

    private struct EntityTypeData
    {
        public string registryName;
        public Type classType;
        public bool isStatic;
        public Func<Entity> factory;
        public List<Entity> entities;
    }

    //Helpers
    // FOR MODDING SUPPORT, SHOULD ADD METHODS FOR REMOVING AND CLEARING TAGS AND SIMILLAR
    public abstract class EntityBuilder
    {
        public Type entityClassType;
        public string registryName;
        public ushort registryId;
        internal abstract Delegate BuildDelegate();
    }
    public abstract class EntityBuilder<T> : EntityBuilder where T : Entity
    {
        protected EntityBuilder()
        {
            entityClassType = typeof(T);
        }
        protected Func<T> factory;
        public List<ushort> staticTags = null;
        public List<ushort> tags = null;

        internal override Delegate BuildDelegate() => Build();

        public EntityBuilder<T> Tagged(string tag, bool isStaticTag)
        {
            if(TryGetTag(tag, out ushort tagId))
            {
                if(isStaticTag)
                    staticTags.Add(tagId);
                else
                    tags.Add(tagId);

                return this;
            }

            throw new Exception($"Tag {tag} does not exist!");
        }
        public EntityBuilder<T> ClearTags(bool staticTags)
        {
            if(staticTags)
                this.staticTags = new();
            else
                this.tags = new();

            return this;
        }

        public virtual Func<T> Build()
        {
            if (factory == null)
                throw new Exception("Factory must be provided before building EntityType.");

            return factory;
        }
    }
    public class DynamicBuilder<T> : EntityBuilder<T> where T : DynamicEntity
    {
        public Vector2? scale = null;

        public EntityBuilder<T> Sized(float width, float height)
        {
            customized = true;
            scale = new Vector2(width, height);
            return this;
        }
        public static DynamicBuilder<T> Of(Func<T> factory)
        {
            var builder = new DynamicBuilder<T>();
            builder.factory = factory;
            return builder;
        }

        public override Func<T> Build()
        {
            var fact = base.Build();
            
        }
    }
    public class StaticBuilder<T> : EntityBuilder<T> where T : StaticEntity
    {
        public static StaticBuilder<T> Of(Func<T> factory)
        {
            var builder = new StaticBuilder<T>();
            builder.factory = factory;
            return builder;
        }
    }

    //Instances
    private static Dictionary<uint, DynamicEntity> dynamicEntities = new();
    private static Dictionary<uint, StaticEntity> staticEntities = new();
    private static List<StaticEntityData> staticEntitiesDatas = new();



    private struct StaticEntityData
    {
        public Vector2 position;
        public float rotation;
        public Vector2 scale;
    }
}
