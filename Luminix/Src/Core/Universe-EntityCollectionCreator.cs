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
        MethodInfo[] GetMethodInfos(EntitySystem entitySystem, string methodName)
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

            return result.ToArray();
        }
        EntityContainer.Method[] GetMethods(EntitySystem entitySystem, string methodName, Dictionary<Type, IComponentContainer> componentContainerMap, bool isByInOnly)
        {
            try
            {
                MethodInfo[] methodInfos = GetMethodInfos(entitySystem, methodName);
                List<EntityContainer.Method> updateMethods = null;

                for(int m = 0; m < methodInfos.Length; m++)
                {
                    try
                    {
                        EntityContainer.Method method = new EntityContainer.Method(methodInfos[m], entitySystem, componentContainerMap, isByInOnly);
                        updateMethods ??= new List<EntityContainer.Method>(methodInfos.Length);
                        updateMethods.Add(method);
                    }
                    catch(Exception ex)
                    {
                        var paramList = string.Join(", ", methodInfos[m].GetParameters()
                            .Select(p => $"{(p.ParameterType.IsByRef ? "ref " : "")}{(p.ParameterType.GetElementType() ?? p.ParameterType).Name} {p.Name}"));

                        Logger.Get().NewLog(Logger.Log.Level.Error, $"Failed to bind method '{methodInfos[m].DeclaringType?.Name}.{methodInfos[m].Name}({paramList})': {ex.Message}");
                    }
                }

                return updateMethods?.ToArray() ?? null;
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
            EntitySystem entitySystem = entityType.GetEntitySystem();

            IComponentContainer[] componentContainerArray = entityType.GetComponentContainers();
        
            Dictionary<Type, IComponentContainer> componentContainerMap = new Dictionary<Type, IComponentContainer>();
            for(int i = 0; i < componentContainerArray.Length; i++)
            {
                componentContainerMap ??= [];
                componentContainerMap.Add(componentContainerArray[i].ComponentType.Type, componentContainerArray[i]);
            }

            EntityContainer.Method[] startMethods = GetMethods(entitySystem, "Start", componentContainerMap, false);
            EntityContainer.Method[] updateMethods = GetMethods(entitySystem, "Update", componentContainerMap, false);
            EntityContainer.Method[] renderMethods = GetMethods(entitySystem, "Render", componentContainerMap, true);
            EntityContainer.Method[] destroyMethods = GetMethods(entitySystem, "OnDestroy", componentContainerMap, false);

            entityContainers.Add(new EntityContainer(entityType, entitySystem, startMethods, updateMethods, renderMethods, destroyMethods, componentContainerArray));
            entryIndex++;
        }

        return entityContainers.ToArray();
    }
}