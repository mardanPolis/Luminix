using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Luminix.Collections;

namespace Luminix;

public sealed partial class Universe
{
    
    internal EntityContainer[] CreateEntityContainers(ReadOnlySpan<EntityType> entityTypes)
    {
        IStructContainer[] GetStructContainers(Type[] structTypes)
        {
            List<IStructContainer> structContainers = new List<IStructContainer>(structTypes.Length);

            IStructContainer idContainer = (IStructContainer)Activator.CreateInstance(typeof(StructContainer<ID>));
            structContainers.Add(idContainer);

            IStructContainer boolContainer = (IStructContainer)Activator.CreateInstance(typeof(StructContainer<bool>));
            structContainers.Add(boolContainer);

            foreach(Type structType in structTypes)
            {
                if(!structType.IsValueType)
                {
                    Logger.Get().NewLog(Logger.Log.Level.Error, $"Unable to create component container for type '{structType.FullName}', reason: Provided type is not a ValueType!");
                    continue;
                }

                if(structType == typeof(ID) || structType == typeof(bool))
                {
                    Logger.Get().NewLog(Logger.Log.Level.Error, $"Unable to create component container for type '{structType.FullName}', reason: Provided type is built in!");
                    continue;
                }

                Type closedType = typeof(StructContainer<>).MakeGenericType(structType);
                IStructContainer structContainer = (IStructContainer)Activator.CreateInstance(closedType);

                structContainers.Add(structContainer);
            }

            return structContainers.ToArray();
        }
        List<MethodInfo> GetMethodInfos(EntitySystem entitySystem, string methodName)
        {
            var result = new List<MethodInfo>();
            var seenVirtualSlots = new HashSet<MethodInfo>();

            Type current = entitySystem.GetType();
            while(current != null && current != typeof(EntitySystem))
            {
                var methodInfos = current.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly)
                    .Where(m => m.Name == methodName);

                foreach(var method in methodInfos)
                {
                    if(method.IsVirtual)
                    {
                        var baseDef = method.GetBaseDefinition();
                        if (seenVirtualSlots.Contains(baseDef))
                            continue;

                        seenVirtualSlots.Add(baseDef);
                    }

                    result.Add(method);
                }

                current = current.BaseType;
            }

            return result;
        }
        List<EntityContainer.Method> GetMethods(EntitySystem entitySystem, string methodName, Dictionary<Type, IStructContainer> structContainerMap, bool isByInOnly)
        {
            try
            {
                List<MethodInfo> methodInfos = GetMethodInfos(entitySystem, methodName);
                List<EntityContainer.Method> updateMethods = null;

                for(int m = 0; m < methodInfos.Count; m++)
                {
                    try
                    {
                        EntityContainer.Method method = new EntityContainer.Method(methodInfos[m], entitySystem, structContainerMap, isByInOnly);
                        updateMethods ??= new List<EntityContainer.Method>(methodInfos.Count);
                        updateMethods.Add(method);
                    }
                    catch(Exception ex)
                    {
                        var paramList = string.Join(", ", methodInfos[m].GetParameters()
                            .Select(p => $"{(p.ParameterType.IsByRef ? "ref " : "")}{(p.ParameterType.GetElementType() ?? p.ParameterType).Name} {p.Name}"));

                        Logger.Get().NewLog(Logger.Log.Level.Error, $"Failed to bind method '{methodInfos[m].DeclaringType?.Name}.{methodInfos[m].Name}({paramList})': {ex.Message}");
                    }
                }

                return updateMethods;
            }
            catch(Exception ex)
            {
                Logger.Get().NewLog(Logger.Log.Level.Error, $"Failed to get {methodName} methods of {entitySystem.GetType().Name}, reason: {ex.Message}");
                return null;
            }
        }

        List<EntityContainer> entityContainers = new List<EntityContainer>(entityTypes.Length);

        uint entryIndex = 0;
        foreach(EntityType entityType in entityTypes)
        {
            EntitySystem entitySystem = entityType.EntitySystem;
            Type[] structTypes = entitySystem.GetComponentTypes();

            IStructContainer[] structContainerArray = GetStructContainers(structTypes);

            Dictionary<Type, IStructContainer> structContainerMap = new Dictionary<Type, IStructContainer>();
            for(int i = 0; i < structContainerArray.Length; i++)
            {
                structContainerMap ??= [];
                structContainerMap.Add(structContainerArray[i].StructType, structContainerArray[i]);
            }

            // var components = entityType.ComponentTypes;
            // foreach(var component in components)
            // {
            //     if(component.Type == typeof(ID) || component.Type == typeof(int))
            //         continue;

            //     if(!structContainerMap.ContainsKey(component.Type, out var container)) 
            //     {
            //         logger.NewLog(Logger.Log.Level.Warning, $"EntityType {entityType.Name} does not have component of type {component.Type}"); 
            //         continue;
            //     }

            //     component.ApplyTo(container);
            // }

            List<EntityContainer.Method> startMethods = GetMethods(entitySystem, "Start", structContainerMap, false);
            List<EntityContainer.Method> updateMethods = GetMethods(entitySystem, "Update", structContainerMap, false);
            List<EntityContainer.Method> renderMethods = GetMethods(entitySystem, "Render", structContainerMap, true);
            List<EntityContainer.Method> destroyMethods = GetMethods(entitySystem, "OnDestroy", structContainerMap, false);

            entityContainers.Add(new EntityContainer(entityType, entitySystem, startMethods.ToArray(), updateMethods.ToArray(), renderMethods.ToArray(), destroyMethods.ToArray(), structContainerArray));
            entryIndex++;
        }

        return entityContainers.ToArray();
    }
}