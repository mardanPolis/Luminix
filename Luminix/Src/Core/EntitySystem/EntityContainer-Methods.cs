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

        private readonly ComponentContainer<EntityID> entityIdContainer;

        public Method(EntityType entityType, MethodInfo methodInfo, EntitySystem entitySystem, Dictionary<Type, IComponentContainer> componentContainers, bool isByInOnly)
        {
            Info = methodInfo;
            Parameters = methodInfo.GetParameters();
            entityIdContainer = (ComponentContainer<EntityID>)componentContainers[typeof(EntityID)];

            var parameterTypes = new DenseList<Type>();

            var instanceConst = Expression.Constant(entitySystem);

            var indexParam = Expression.Parameter(typeof(int), "i");
            var elementAccesses = new Expression[Parameters.Length];

            for (int i = 0; i < Parameters.Length; i++)
            {
                ParameterInfo parameterInfo = Parameters[i];
                Type parameterType = parameterInfo.ParameterType;
                Type elementType = parameterType.GetElementType();

                if (!componentContainers.TryGetValue(elementType, out var container))
                    throw new InvalidOperationException($"{entityType.Name} has no component with Type {elementType.Name}!");
                
                if (parameterInfo.IsOut)
                    throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must not be passed with 'out' parameter modifier!");

                if(!parameterTypes.TryAdd(elementType))
                    throw new InvalidOperationException($"Repeating parameters with the same type {elementType.Name}!");

                var componentType = container.ComponentType;

                if (elementType == typeof(EntityID))
                {
                    if (parameterType.IsByRef)
                        throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by value.");
                }
                else
                {
                    if(!parameterType.IsByRef && componentType.Size > 8)
                    {
                        if(isByInOnly)
                            throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by 'in' parameter modifier!");

                        throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by 'ref'/'in' parameter modifier!");
                    }

                    if(isByInOnly && !parameterInfo.IsIn && componentType.Size > 8)
                        throw new InvalidOperationException($"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' must be passed by 'in' parameter modifier!");

                    if (parameterInfo.IsIn && componentType.Size <= 8)
                    {
                        Logger.NewLog(Logger.Log.Level.Warning, $"Parameter '{parameterInfo.Name}' on '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' is 'in {elementType.Name}' ({componentType.Size} bytes) — no benefit over pass-by-value below 8 bytes, consider removing 'in'.");
                    }
                }

                var concreteContainerType = TypeRegistry.GetClosedGenericType(typeof(ComponentContainer<>), elementType);
                var dataField = concreteContainerType.GetProperty(nameof(ComponentContainer<int>.Data), BindingFlags.Instance | BindingFlags.Public);
                var containerConst = Expression.Constant(container, concreteContainerType);
                var arrayAccess = Expression.Property(containerConst, dataField);
                elementAccesses[i] = Expression.ArrayAccess(arrayAccess, indexParam);
            }

            var call = Expression.Call(instanceConst, methodInfo, elementAccesses);
            var lambda = Expression.Lambda<Action<int>>(call, indexParam);
            compiledPerIndex = lambda.Compile();
        }

        private Action<int> compiledPerIndex;

        public void Invoke(int index)
        {
            InvokeAction(index);
        }
        public void InvokeFor(int length)
        {
            for (int i = 0; i < length; i++)
            {
                InvokeAction(i);
            }
        }
        public void InvokeBackFor(int entityCount, int length)
        {
            for (int i = 1; i < length; i++)
            {
                InvokeAction(entityCount - i);
            }
        }

        private void InvokeAction(int index)
        {
            try
            {
                compiledPerIndex(index);
            }
            catch(Exception ex)
            {
                var entity = entityIdContainer.Data[index].Value;
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to {Info.DeclaringType}.{Info.Name} on Entity {entity}, reason: {ex}");
            }
        }

        public override string ToString()
        {
            return $"{Info.Name} Method";
        }
    }
}