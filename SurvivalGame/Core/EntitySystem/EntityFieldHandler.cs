using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SurvivalGame.Core.EntitySystem;

internal static class EntityFieldHandler
{
    public struct FieldAccessor()
    {
        public Func<Entity, Entity> getter;
        public Action<Entity, Entity> setter;
    }
    public struct StaticFieldAccessor()
    {
        public Func<Entity> getter;
        public Action<Entity> setter;
    }
    
    private static StaticFieldAccessor[] staticFieldAccessors;

    public static bool CreateAccessors(Entity.Type entityType, Type entityClass, out FieldAccessor[] _fieldAccessors)
    {
        List<FieldAccessor> accessors = [];

        Type current = entityClass;
        while (current != null && current != typeof(object))
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!typeof(Entity).IsAssignableFrom(field.FieldType))
                    continue;

                var accessor = new FieldAccessor();

                // Getter
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var castInstance = Expression.Convert(instanceParam, entityClass);
                var fieldAccess = Expression.Field(castInstance, field);
                var castResult = Expression.Convert(fieldAccess, typeof(Entity));
                var getter = Expression.Lambda<Func<object, Entity>>(castResult, instanceParam).Compile();

                // Setter
                var valueParam = Expression.Parameter(typeof(Entity), "value");
                var assign = Expression.Assign(fieldAccess, Expression.Convert(valueParam, field.FieldType));
                var setter = Expression.Lambda<Action<object, Entity>>(assign, instanceParam, valueParam).Compile();

                accessor.getter = getter;
                accessor.setter = setter;
                accessors.Add(accessor);
            }

            current = current.BaseType;
        }

        _fieldAccessors = accessors.ToArray();

        return _fieldAccessors.Length > 0;
    }

    public static void CreateStaticAccessors()
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

                staticFieldAccessors.Add(new StaticFieldAccessor
                {
                    getter = getter,
                    setter = setter
                });
            }
        }

        EntityFieldHandler.staticFieldAccessors = staticFieldAccessors.ToArray();
    }

    // public static ICollection<Entity.Type> GetEntityTypesWithAccessors()
    // {
    //     return entityTypeFieldAccessors.Keys;
    // }
    // public static ICollection<FieldAccessor[]> GetFieldAccessors()
    // {
    //     return entityTypeFieldAccessors.Values;
    // }
}