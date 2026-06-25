using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Luminix.Collections;
using Luminix.Identifiers;

namespace Luminix;

internal struct EntityType
{
    public class UpdateMethod
    {
        
        public UpdateMethod(MethodInfo info, EntitySystem entitySystem, IComponentContainer[] componentContainers)
        {
            this.Info = info;
            this.Parameters = info.GetParameters();
            this.ParameterTypes = new DenseList<Type>();

            this.elementTypes = new Type[Parameters.Length];
            this.boundContainers = new IComponentContainer[Parameters.Length];

            bool indexParameterExists = false;

            var componentTypeMap = new Dictionary<Type, IComponentContainer>();
            for (int i = 0; i < componentContainers.Length; i++)
                componentTypeMap[componentContainers[i].ComponentType] = componentContainers[i];

            for (int i = 0; i < Parameters.Length; i++)
            {
                ParameterInfo parameter = Parameters[i];
                Type parameterType = parameter.ParameterType;

                // entity index parameter
                if (!parameterType.IsByRef && parameterType == typeof(int) && parameter.Name == "entityIndex")
                {
                    if(indexParameterExists)
                        throw new InvalidOperationException($"Repeating index parameters!");
                    
                    indexParameterExists = true;
                    elementTypes[i] = typeof(int);
                    continue;
                }

                var elementType = parameterType.GetElementType()
                    ?? throw new InvalidOperationException(
                        $"Parameter '{Parameters[i].Name}' on '{info.DeclaringType?.Name}.{info.Name}' must be passed by ref!");

                if(ParameterTypes.Contains(elementType))
                    throw new InvalidOperationException($"Repeating parameters with the same type {elementType.Name}!");

                // entity parameter
                if (elementType == typeof(Entity))
                {
                    elementTypes[i] = elementType;
                    continue;
                }

                // component parameter
                if (!componentTypeMap.TryGetValue(elementType, out var container))
                    throw new InvalidOperationException($"EntityType has no component with Type {elementType.Name}!");

                ParameterTypes.Add(elementType); 
                elementTypes[i] = elementType;
                boundContainers[i] = container;
            }

            this.instanceConst = Expression.Constant(entitySystem);
            this.indexParam = Expression.Parameter(typeof(int), "i");
            this.elementAccesses = new Expression[elementTypes.Length];
        }

        public MethodInfo Info { get; }
        public ParameterInfo[] Parameters { get; }
        public DenseList<Type> ParameterTypes { get; }

        private readonly Type[] elementTypes;
        private readonly Expression[] elementAccesses;
        private readonly ConstantExpression instanceConst;
        private readonly ParameterExpression indexParam;

        private readonly IComponentContainer[] boundContainers; // parallel to Parameters, resolved once
        private Action<int> compiledPerIndex;

        public void Invoke(int entityCount)
        {
            for (int i = 0; i < entityCount; i++)
                compiledPerIndex(i);
        }

        public void Compile(Entity[] entities)
        {
            var entitiesConst = Expression.Constant(entities, typeof(Entity[]));

            for (int i = 0; i < elementTypes.Length; i++)
            {
                var elemType = elementTypes[i];
                var parameter = Parameters[i];

                if(elemType == typeof(int) && parameter.Name == "entityIndex")
                {
                    elementAccesses[i] = indexParam;
                    continue;
                }

                if(elemType == typeof(Entity))
                {
                    elementAccesses[i] = Expression.ArrayAccess(entitiesConst, indexParam);
                    continue;
                }

                Array array = boundContainers[i].GetArray();
                var arrType = elemType.MakeArrayType();
                var arrConst = Expression.Constant(array, arrType);
                elementAccesses[i] = Expression.ArrayAccess(arrConst, indexParam);
            }

            var call = Expression.Call(instanceConst, Info, elementAccesses);
            var lambda = Expression.Lambda<Action<int>>(call, indexParam);
            compiledPerIndex = lambda.Compile();
        }
    }

    public EntityType(string typeName, Entity.TypeID typeID, EntitySystem entitySystem, UpdateMethod[] updateMethods, IComponentContainer[] componentContainers)
    {
        int defaultEntityCount = 8;

        this.TypeName = typeName;
        this.TypeID = typeID;
        this.entities = new Entity[defaultEntityCount];
        this.EntitySystem = entitySystem;
        this.updateMethods = updateMethods;

        this.componentTypeMap = new Dictionary<Type, int>();
        this.componentContainers = componentContainers;
        for(int i = 0; i < componentContainers.Length; i++)
        {
            var container = componentContainers[i];
            var updateMethod = updateMethods[i];
            componentTypeMap.Add(container.ComponentType, i);
            container.Reset(defaultEntityCount);
        }

        for(int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].Compile(this.entities);
    }

    public string TypeName { get; }
    public Entity.TypeID TypeID { get; }
    public EntitySystem EntitySystem { get; }

    public Entity[] Entities => entities;
    public int EntityCount { get; private set; }

    private Entity[] entities;
    private readonly Dictionary<Type, int> componentTypeMap;
    private readonly IComponentContainer[] componentContainers;

    private UpdateMethod[] updateMethods;
    private bool dirtyUpdateMethods = false;

    public void CreateEntity(ID entityID)
    {
        if(EntityCount >= entities.Length)
        {
            int newLength = entities.Length * 2;
            Array.Resize(ref entities, newLength);
            for(int i = 0; i < componentContainers.Length; i++)
                componentContainers[i].Resize(newLength);
            
            dirtyUpdateMethods = true;
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

    public void UpdateEntities()
    {
        var entityCount = EntityCount;

        if (dirtyUpdateMethods)
        {
            for (int i = 0; i < updateMethods.Length; i++)
                updateMethods[i].Compile(entities);
            dirtyUpdateMethods = false;
        }

        for (int i = 0; i < updateMethods.Length; i++)
            updateMethods[i].Invoke(entityCount);
    }
}