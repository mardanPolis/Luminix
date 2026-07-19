using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Luminix.Collections;

namespace Luminix;

internal partial struct EntityType
{
    internal sealed class Method
    {
        private enum ParamType
        {
            NonRefParam,
            EntityParam,
            OtherParam,
        };

        public MethodInfo Info { get; init; }
        public ParameterInfo[] Parameters { get; init;}

        private Type[] elementTypes { get; init; }
        private ParamType[] elementParamTypes { get; init; }
        private Expression[] elementAccesses { get; init; }
        private ConstantExpression instanceConst { get; init; }
        private ParameterExpression indexParam { get; init; }

        private IComponentContainer[] boundContainers { get; init; } // parallel to Parameters, resolved once

        public Method(MethodInfo info, EntitySystem entitySystem, Dictionary<Type, IComponentContainer> componentContainerMap)
        {
            Info = info;
            Parameters = info.GetParameters();
            var parameterTypes = new DenseList<Type>();

            instanceConst = Expression.Constant(entitySystem);
            indexParam = Expression.Parameter(typeof(int), "i");

            elementTypes = new Type[Parameters.Length];
            elementParamTypes = new ParamType[Parameters.Length];
            elementAccesses = new Expression[Parameters.Length];
            boundContainers = new IComponentContainer[Parameters.Length];

            for (int i = 0; i < Parameters.Length; i++)
            {
                ParameterInfo parameterInfo = Parameters[i];
                Type parameterType = parameterInfo.ParameterType;

                ParamType paramType = GetParamType(parameterInfo);
                var elementType = parameterType.GetElementType();
                
                if(paramType == ParamType.NonRefParam)
                    throw new InvalidOperationException($"Parameter '{Parameters[i].Name}' on '{info.DeclaringType?.Name}.{info.Name}' must be passed by ref!");
                
                if(!parameterTypes.TryAdd(elementType))
                    throw new InvalidOperationException($"Repeating parameters with the same type {elementType.Name}!");

                switch(paramType)
                {
                    case ParamType.EntityParam:
                        elementParamTypes[i] = paramType;
                        elementTypes[i] = elementType;
                    break;
                    case ParamType.OtherParam:
                        if (!componentContainerMap.TryGetValue(elementType, out var container))
                            throw new InvalidOperationException($"EntityType has no component with Type {elementType.Name}!");

                        elementParamTypes[i] = paramType;
                        elementTypes[i] = elementType;
                        boundContainers[i] = container;
                    break;
                }
            }
        }

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

                switch(elementParamTypes[i])
                {
                    case ParamType.EntityParam:
                        elementAccesses[i] = Expression.ArrayAccess(entitiesConst, indexParam);
                    break;
                    case ParamType.OtherParam:
                        Array array = boundContainers[i].GetArray();
                        var arrType = elemType.MakeArrayType();
                        var arrConst = Expression.Constant(array, arrType);
                        elementAccesses[i] = Expression.ArrayAccess(arrConst, indexParam);
                    break;
                }
            }

            var call = Expression.Call(instanceConst, Info, elementAccesses);
            var lambda = Expression.Lambda<Action<int>>(call, indexParam);
            compiledPerIndex = lambda.Compile();
        }
        
        private ParamType GetParamType(ParameterInfo parameterInfo)
        {
            Type parameterType = parameterInfo.ParameterType;
            if(!parameterType.IsByRef)
                return ParamType.NonRefParam;

            if(parameterType.GetElementType() == typeof(Entity))
                return ParamType.EntityParam;
            
            return ParamType.OtherParam;
        }
    }
}