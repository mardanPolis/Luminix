
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Win32;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public static class EntityTypeRegistry
{
    private struct EntityTypeData(string RegistryName, Type ClassType, bool IsStatic, Entity.TypeValues TypeValues, Func<uint, Entity.TypeValues, Entity> Factory)
    {
        public readonly string registryName = RegistryName;
        public readonly Type classType = ClassType ?? throw new Exception("Failed to create EntityTypeData: no classType defined!");
        public readonly bool isStatic = IsStatic;
        public readonly Entity.TypeValues typeValues = TypeValues ?? throw new Exception("Failed to create EntityTypeData: no typeValues defined!");
        public readonly Func<uint, Entity.TypeValues, Entity> factory = Factory ?? throw new Exception("Failed to create EntityTypeData: no factory defined!");
        public readonly HashSet<uint> entities = [];
    }
    private class InitData
    {
        public HashSet<string> registryNames = [];
        public OrderedDictionary<string, EntityTypeBuilder> entityBuilders = [];
    }

    private readonly struct RegistryData(Dictionary<string, Entity.Type> EntityTypes, EntityTypeData[] EntityTypeDatas)
    {
        public readonly Dictionary<string, Entity.Type> entityTypes = EntityTypes;
        public readonly EntityTypeData[] entityTypeDatas = EntityTypeDatas;
        public readonly SortedSet<string> registryNames = [.. EntityTypes.Keys];
        public readonly ushort EntityTypeCount => (ushort)entityTypes.Count;
    }

    private static InitData initData = new();
    private static RegistryData registryData;

    public static void Register(string registryName, EntityTypeBuilder entityBuilder)
    {
        if(!EntityTagRegistry.IsFinalized())
            throw new Exception($"Tried to register entity '{registryName}', but Entity Tag registry isnt finalized!");

        if(IsFinalized())
            throw new Exception($"Tried to register entity '{registryName}', but Entity registry already finalized!");

        var entityBuilders = initData.entityBuilders;
        if(entityBuilders.ContainsKey(registryName))
            throw new Exception($"Entity '{registryName}' already registered");

        if(!entityBuilder.EntityClass.IsSubclassOf(typeof(DynamicEntity)) && !entityBuilder.EntityClass.IsSubclassOf(typeof(StaticEntity)))
            throw new Exception($"Type '{entityBuilder.EntityClass.Name}' is nor a DynamicEntity, nor a StaticEntity!");

        if(entityBuilders.Count >= ushort.MaxValue)
            throw new Exception($"Tried to register '{registryName}', but Entity registry has reached max registry count - {ushort.MaxValue}");

        entityBuilders.Add(registryName, entityBuilder);
    }
    public static bool TryGetBuilder(string registryName, out EntityTypeBuilder _builder)
    {
        if(IsFinalized())
            throw new Exception($"Tried to get entity '{registryName}' builder, but Entity registry already finalized!");

        if (initData.entityBuilders.TryGetValue(registryName, out var baseBuilder) && baseBuilder is EntityTypeBuilder typedBuilder)
        {
            _builder = typedBuilder;
            return true;
        }

        _builder = null;
        return false;
    }
    internal static void FinalizeRegistry()
    {
        if(IsFinalized())
            throw new System.Exception($"Entities registry is already finalized!");

        var entityBuilders = initData.entityBuilders;
        Dictionary<string, Entity.Type> entityTypes = [];
        EntityTypeData[] entityTypeDatas = new EntityTypeData[entityBuilders.Count];

        //Finalizing EntityTypes
        for (ushort i = 0; i < entityBuilders.Count; i++)
        {
            var registryName = entityBuilders.GetAt(i).Key;
            var typeBuilder = entityBuilders.GetAt(i).Value;

            Entity.Type entityType = new(i);
            entityTypes.Add(registryName, entityType);

            entityTypeDatas[i] = new EntityTypeData(
                registryName,
                typeBuilder.EntityClass,
                typeBuilder.EntityClass.IsSubclassOf(typeof(StaticEntity)),
                typeBuilder.BuildTypeValues(entityType),
                typeBuilder.Build()
            );
            
            EntityFieldHandler.CreateAccessors(entityType, typeBuilder.EntityClass, out var fieldAccessors);
        }

        registryData = new(entityTypes, entityTypeDatas);

        EntityFieldHandler.CreateStaticAccessors();

        initData = null;

        SurvivalGame.LOGGER.Log($"Registered EntityTypes ({entityTypes.Count}):");
        for (int i = 0; i < entityTypeDatas.Length; i++)
            SurvivalGame.LOGGER.Log($" - id:{i} -> registryName: {entityTypeDatas[i].registryName}; class: {entityTypeDatas[i].classType.Name}; isStatic: {entityTypeDatas[i].isStatic}; typeValues: {entityTypeDatas[i].typeValues}");
        
    }

    public static TBuilder NewBuilder<TBuilder, TEntity>() where TBuilder : EntityTypeBuilder where TEntity : Entity
    {
        return (TBuilder)Activator.CreateInstance(typeof(TBuilder), typeof(TEntity))!;
    }

    public static bool IsFinalized()
    {
        return initData == null;
    }

    public static bool TryGet(string registryName, out Entity.Type _type)
    {
        return registryData.entityTypes.TryGetValue(registryName, out _type);
    }
    public static bool TryGet(Entity.Type type, out string _registryName)
    {
        _registryName = string.Empty;

        if(registryData.entityTypes.Count <= type.Id)
            return false;

        _registryName = registryData.entityTypeDatas[type.Id].registryName;
        return true;
    }
    public static bool TryGet(Entity.Type type, out Type _classType)
    {
        _classType = null;

        if(registryData.entityTypes.Count <= type.Id)
            return false;

        _classType = registryData.entityTypeDatas[type.Id].classType;
        return true;
    }
    public static bool TryGet(Entity.Type type, out bool _isStatic)
    {
        _isStatic = false;

        if(registryData.entityTypes.Count <= type.Id)
            return false;

        _isStatic = registryData.entityTypeDatas[type.Id].isStatic;
        return true;
    }
    public static bool TryGet(Entity.Type type, out HashSet<uint> _entities)
    {
        _entities = null;

        if(registryData.entityTypes.Count <= type.Id)
            return false;

        _entities = registryData.entityTypeDatas[type.Id].entities;
        return true;
    }

    internal static bool TryGet(string registryName, out HashSet<uint> _entities, out bool _isStatic, out Func<uint, Entity.TypeValues, Entity> _factory, out Entity.TypeValues _values, out Entity.Type _type)
    {
        _isStatic = false;
        _factory = null;
        _values = null;
        _entities = null;

        if(registryData.entityTypes.TryGetValue(registryName, out _type))
        {
            ref EntityTypeData data = ref registryData.entityTypeDatas[_type.Id];
            _isStatic = data.isStatic;
            _factory = data.factory;
            _values = data.typeValues;
            _entities = data.entities;

            return true;
        }

        return false;
    }
    internal static bool TryGet(Entity.Type type, out HashSet<uint> _entities, out bool _isStatic, out Func<uint, Entity.TypeValues, Entity> _factory, out Entity.TypeValues _values)
    {
        _isStatic = false;
        _factory = null;
        _values = null;
        _entities = null;

        if(registryData.entityTypes.Count <= type.Id)
            return false;

        ref EntityTypeData data = ref registryData.entityTypeDatas[type.Id];
        _isStatic = data.isStatic;
        _factory = data.factory;
        _values = data.typeValues;
        _entities = data.entities;

        return true;
    }
    internal static bool TryGet(Entity.Type type, out HashSet<uint> _entities, out bool _isStatic)
    {
        _isStatic = false;
        _entities = null;

        if(registryData.entityTypes.Count <= type.Id)
            return false;

        ref EntityTypeData data = ref registryData.entityTypeDatas[type.Id];
        _isStatic = data.isStatic;
        _entities = data.entities;

        return true;
    }

    public static SortedSet<string> RegistryNames
    {
        get
        {
            return registryData.registryNames;
        }
    }
    public static List<Entity.TypeValues> RegistryTypeValues
    {
        get
        {
            List<Entity.TypeValues> typeValues = [];
            for (int i = 0; i < registryData.entityTypeDatas.Length; i++)
            {
                ref EntityTypeData data = ref registryData.entityTypeDatas[i];
                if(data.typeValues != null)
                    typeValues.Add(data.typeValues);
            }
            return typeValues;
        }
    }

}

public class EntityTypeBuilder
{
    public EntityTypeBuilder(Type entityClass)
    {
        if (SurvivalGame.CurrentGameState != GameState.Initialization)
            throw new Exception($"Cannot create EntityBuilder of type '{entityClass.Name}', because the GameState is not in Initialization.");

        if (!EntityTagRegistry.IsFinalized())
            throw new Exception($"Cannot create EntityBuilder of type '{entityClass.Name}', because Entity Tag registry is not finalized.");

        if (!typeof(Entity).IsAssignableFrom(entityClass))
            throw new Exception($"Cannot create EntityBuilder: type '{entityClass.Name}' must inherit from 'Entity'.");

        if (entityClass.IsAbstract)
            throw new Exception($"Cannot create EntityBuilder: type '{entityClass.Name}' is abstract.");

        EntityClass = entityClass;
    }
    public Type EntityClass { private set; get; } = null;
    protected Type typeValuesClass = null;
    public virtual Type TypeValuesClass { 
        get {
             return typeValuesClass ?? typeof(Entity.TypeValues);
        }
    }
    public HashSet<Entity.Tag> TypeTags { protected set; get; } = null;
    public HashSet<Entity.Tag> Tags { protected set; get; } = null;
    public Vector2? Scale = null;

    public virtual EntityTypeBuilder TypeValuesAs(Type typeValuesClass)
    {
        if(!typeValuesClass.IsAbstract)
            throw new Exception("Provided Type cant be abstract!");

        if(!typeValuesClass.IsSubclassOf(typeof(Entity.TypeValues)))
            throw new Exception("Provided Type must inherit from 'Entity.TypeValues'!");

        this.typeValuesClass = typeValuesClass;

        return this;
    }

    public virtual EntityTypeBuilder Tagged(string tag, bool isTypeTag)
    {
        if(isTypeTag && TypeValuesClass == null)
            throw new Exception($"Unable to Tag the Type, because the TypeValues class is not set.");

        if(EntityTagRegistry.TryGet(tag, out Entity.Tag tagId))
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
    public virtual EntityTypeBuilder UnTagged(string tag)
    {
        if(EntityTagRegistry.TryGet(tag, out Entity.Tag tagId))
        {
            Tags?.Remove(tagId);
            TypeTags?.Remove(tagId);
        }
        
        return this;
    }
    public virtual EntityTypeBuilder ClearTags(bool typeTags)
    {   
        if(!typeTags)
            Tags = null;
        else
            TypeTags = null;
            
        return this;
    }

    public virtual Func<uint, Entity.TypeValues, Entity> Build()
    {
        //Getting Entity Class constructor new Entity(id)
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