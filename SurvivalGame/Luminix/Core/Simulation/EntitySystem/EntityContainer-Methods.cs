using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Luminix.Collections;
using Luminix.Identifiers;

namespace Luminix;

internal partial struct EntityContainer
{
    internal sealed class Method
    {
        private enum ParamType
        {
            NonRefParam,
            IDParam,
            OtherParam,
        };

        public MethodInfo Info { get; init; }
        public ParameterInfo[] Parameters { get; init;}

        private Type[] elementTypes { get; init; }
        private ParamType[] elementParamTypes { get; init; }
        private Expression[] elementAccesses { get; init; }
        private ConstantExpression instanceConst { get; init; }
        private ParameterExpression indexParam { get; init; }

        private IStructContainer[] boundContainers { get; init; } // parallel to Parameters, resolved once

        public Method(MethodInfo info, EntitySystem entitySystem, Dictionary<Type, IStructContainer> structContainerMap)
        {
            Info = info;
            Parameters = info.GetParameters();
            var parameterTypes = new DenseList<Type>();

            instanceConst = Expression.Constant(entitySystem);
            indexParam = Expression.Parameter(typeof(int), "i");

            elementTypes = new Type[Parameters.Length];
            elementParamTypes = new ParamType[Parameters.Length];
            elementAccesses = new Expression[Parameters.Length];
            boundContainers = new IStructContainer[Parameters.Length];

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
                    case ParamType.IDParam:
                        elementParamTypes[i] = paramType;
                        elementTypes[i] = elementType;
                    break;
                    case ParamType.OtherParam:
                        if (!structContainerMap.TryGetValue(elementType, out var container))
                            throw new InvalidOperationException($"EntityType has no component with Type {elementType.Name}!");

                        elementParamTypes[i] = paramType;
                        elementTypes[i] = elementType;
                        boundContainers[i] = container;
                    break;
                }
            }
        }

        private Action<int> compiledPerIndex;

        public void Invoke(int index)
        {
            compiledPerIndex(index);
        }
        public void InvokeFor(int entityCount)
        {
            for (int i = 0; i < entityCount; i++)
                compiledPerIndex(i);
        }

        public void Compile()
        {
            for (int i = 0; i < elementTypes.Length; i++)
            {
                var elemType = elementTypes[i];
                Array array = boundContainers[i].GetArray();
                var arrType = elemType.MakeArrayType();
                var arrConst = Expression.Constant(array, arrType);
                elementAccesses[i] = Expression.ArrayAccess(arrConst, indexParam);
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

            if(parameterType.GetElementType() == typeof(ID))
                return ParamType.IDParam;
            
            return ParamType.OtherParam;
        }
    }
}