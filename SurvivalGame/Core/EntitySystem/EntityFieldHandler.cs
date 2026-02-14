using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SurvivalGame.Core.EntitySystem;

internal static class EntityFieldHandler
{
    public struct FieldAccessor(string fieldName, Func<Entity, Entity> getter, Action<Entity, Entity> setter)
    {
        private readonly Func<Entity, Entity> getter = getter;
        private readonly Action<Entity, Entity> setter = setter;
        private readonly string fieldName = fieldName;

        public Entity Get(Entity tar) => getter(tar);
        public void Set(Entity tar, Entity value) => setter(tar, value);
        public string FieldName => fieldName;
    }
    public struct StaticFieldAccessor(string fieldName, Func<Entity> getter, Action<Entity> setter)
    {
        private readonly Func<Entity> getter = getter;
        private readonly Action<Entity> setter = setter;
        private readonly string fieldName = fieldName;

        public Entity Get() => getter();
        public void Set(Entity value) => setter(value);
        public readonly string FieldName => fieldName;
    }
    
    private static (Entity.Type EntityType, FieldAccessor[] Accessors)[] fieldAccessors = null;
    private static StaticFieldAccessor[] staticFieldAccessors;

    public static void ClearInvalidReferences()
    {
        foreach (var staticAccessor in staticFieldAccessors)
        {
            var value = staticAccessor.Get();
            if (value is not null && value.isDestroyed)
                staticAccessor.Set(null);
        }

        foreach (var (entityType, accessors) in fieldAccessors)
        {
            EntityTypeRegistry.Visit(entityType, (entity) =>
            {
                foreach (var accessor in accessors)
                {
                    var value = accessor.Get(entity);
                    if (value is not null && value.isDestroyed)
                        accessor.Set(entity, null);
                }
            });
        }
    }

    public static void CreateAccessors()
    {
        if(EntityFieldHandler.staticFieldAccessors != null || EntityFieldHandler.fieldAccessors != null)
            throw new Exception("Tried to create Entity field accessors, but they have already been created!");

        CreateStaticAccessors(out bool staticResult, out var staticFieldAccessors);

        List<(Entity.Type EntityType, FieldAccessor[] Accessors)> fieldAccessors = [];
        EntityTypeRegistry.Visit((entityType, registryName, entityClass, isStatic) =>
        {
            CreateInstanceAccessors(entityClass, out bool result, out FieldAccessor[] accessors);
            if (result)
                fieldAccessors.Add((entityType, accessors));
        });

        foreach(var accessor in staticFieldAccessors)
            SurvivalGame.LOGGER.Log($"Registered static field accessor for field '{accessor.FieldName}'");

        foreach(var (entityType, accessors) in fieldAccessors)
        {
            foreach(var accessor in accessors)
                SurvivalGame.LOGGER.Log($"Registered field accessor for EntityType '{entityType.Id}' and field '{accessor.FieldName}'");
        }

        EntityFieldHandler.fieldAccessors = fieldAccessors.ToArray();
        EntityFieldHandler.staticFieldAccessors = staticFieldAccessors;
    }
    private static void CreateInstanceAccessors(Type entityClass, out bool _result, out FieldAccessor[] _accessors)
    {
        List<FieldAccessor> accessors = [];

        Type current = entityClass;
        while (current != null && current != typeof(Entity))
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!typeof(Entity).IsAssignableFrom(field.FieldType))
                    continue;

                // Getter
                var instanceParam = Expression.Parameter(typeof(Entity), "instance");
                var castInstance = Expression.Convert(instanceParam, entityClass);
                var fieldAccess = Expression.Field(castInstance, field);
                var castResult = Expression.Convert(fieldAccess, typeof(Entity));
                var getter = Expression.Lambda<Func<Entity, Entity>>(castResult, instanceParam).Compile();

                // Setter
                var valueParam = Expression.Parameter(typeof(Entity), "value");
                var assign = Expression.Assign(fieldAccess, Expression.Convert(valueParam, field.FieldType));
                var setter = Expression.Lambda<Action<Entity, Entity>>(assign, instanceParam, valueParam).Compile();

                var accessor = new FieldAccessor(field.Name, getter, setter);
                accessors.Add(accessor);
            }

            current = current.BaseType;
        }

        _result = accessors.Count > 0;
        _accessors = accessors.ToArray();
    }
    private static void CreateStaticAccessors(out bool _result, out StaticFieldAccessor[] _staticAccessors)
    {
        List<StaticFieldAccessor> staticFieldAccessors = [];

        var entityType = typeof(Entity);
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => entityType.IsAssignableFrom(t));

        foreach (var type in allTypes)
        {
            var staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in staticFields)
            {
                if (!typeof(Entity).IsAssignableFrom(field.FieldType))
                    continue;

                // Getter
                var fieldAccess = Expression.Field(null, field);
                var castResult = Expression.Convert(fieldAccess, typeof(Entity));
                var getter = Expression.Lambda<Func<Entity>>(castResult).Compile();

                // Setter
                var valueParam = Expression.Parameter(typeof(Entity), "value");
                var assign = Expression.Assign(fieldAccess, Expression.Convert(valueParam, field.FieldType));
                var setter = Expression.Lambda<Action<Entity>>(assign, valueParam).Compile();

                staticFieldAccessors.Add(new StaticFieldAccessor(field.Name, getter, setter));
            }
        }

        EntityFieldHandler.staticFieldAccessors = staticFieldAccessors.ToArray();

        _result = staticFieldAccessors.Count > 0;
        _staticAccessors = staticFieldAccessors.ToArray();
    }
}