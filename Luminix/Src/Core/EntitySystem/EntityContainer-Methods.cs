using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Luminix.Collections;

namespace Luminix;

internal partial class EntityContainer
{
    internal sealed class Method
    {
        public MethodInfo Info { get; }
        public ParameterInfo[] Parameters { get; }

        private Type[] elementTypes { get; }

        public Method(MethodInfo methodInfo, EntitySystem entitySystem, Dictionary<Type, IStructContainer> structContainers, bool isByInOnly)
        {
            Info = methodInfo;
            Parameters = methodInfo.GetParameters();
            var parameterTypes = new DenseList<Type>();

            var instanceConst = Expression.Constant(entitySystem);

            var indexParam = Expression.Parameter(typeof(int), "i");
            var elementAccesses = new Expression[Parameters.Length];

            elementTypes = new Type[Parameters.Length];

            for (int i = 0; i < Parameters.Length; i++)
            {
                ParameterInfo parameterInfo = Parameters[i];
                Type parameterType = parameterInfo.ParameterType;
                Type elementType = parameterType.GetElementType();

                if (!structContainers.TryGetValue(elementType, out var container))
                    throw new InvalidOperationException($"EntityType has no component with Type {elementType.Name}!");
                
                if (parameterInfo.IsOut)
                    throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must not be passed with 'out' parameter modifier!");

                if(!parameterTypes.TryAdd(elementType))
                    throw new InvalidOperationException($"Repeating parameters with the same type {elementType.Name}!");

                var structSize = container.StructSize;

                if (elementType == typeof(ID))
                {
                    if (parameterType.IsByRef)
                        throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by value.");
                }
                else
                {
                    if(!parameterType.IsByRef && structSize > 8)
                    {
                        if(isByInOnly)
                            throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by 'in' parameter modifier!");

                        throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by 'ref'/'in' parameter modifier!");
                    }

                    if(isByInOnly && !parameterInfo.IsIn && structSize > 8)
                        throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by 'in' parameter modifier!");

                    if (parameterInfo.IsIn && structSize <= 8)
                    {
                        Logger.Get().NewLog(Logger.Log.Level.Warning, $"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' is 'in {elementType.Name}' ({structSize} bytes) — no benefit over pass-by-value below 8 bytes, consider removing 'in'.");
                    }
                }

                var elemType = elementTypes[i];
                var concreteContainerType = typeof(StructContainer<>).MakeGenericType(elemType);
                var dataField = concreteContainerType.GetField(nameof(StructContainer<int>.Data))!;
                var containerConst = Expression.Constant(container, concreteContainerType);
                var arrayAccess = Expression.Field(containerConst, dataField);
                elementAccesses[i] = Expression.ArrayAccess(arrayAccess, indexParam);
            }

            var call = Expression.Call(instanceConst, methodInfo, elementAccesses);
            var lambda = Expression.Lambda<Action<int>>(call, indexParam);
            compiledPerIndex = lambda.Compile();
        }

        private Action<int> compiledPerIndex;

        public void Invoke(int index)
        {
            compiledPerIndex(index);
        }
        public void InvokeFor(int length)
        {
            for (int i = 0; i < length; i++)
                compiledPerIndex(i);
        }
        public void InvokeBackFor(int entityCount, int length)
        {
            for (int i = 1; i < length; i++)
                compiledPerIndex(entityCount - i);
        }
    }
}