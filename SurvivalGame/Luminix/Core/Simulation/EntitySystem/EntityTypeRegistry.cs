using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Luminix;

public sealed class EntityTypeRegistry : Workspace.Service
{
    private record struct Entry(string TypeName, Type EntitySystemType);
    private record struct FinalEntry(string TypeName, Entity.TypeID TypeID, EntitySystem EntitySystem, EntityType.Method[] UpdateMethods, EntityType.Method RenderMethod, IComponentContainer[] ComponentContainers);

    private Dictionary<string, Entry> entries = new();
    private FinalEntry[] finalEntries = null;
    private bool isFinalized => finalEntries != null;

    public void Register<TEntitySystem>(string typeName) where TEntitySystem : EntitySystem
    {
        typeName = typeName.ToLower();
        try
        {
            if(isFinalized)
                throw new Exception($"Registry has been already finalized!");

            if(string.IsNullOrWhiteSpace(typeName))
                throw new Exception($"Provided type name is null, empty or consists only of white-space characters!");

            if(entries.ContainsKey(typeName))
                throw new Exception($"Entry with the same name already exists!");

            Type systemType = typeof(TEntitySystem);
            if(!systemType.IsClass)
                throw new Exception($"Provided EntitySystem '{systemType.Name}' type is not a class!");

            if(systemType.IsAbstract)
                throw new Exception($"Provided EntitySystem '{systemType.Name}' type is an abstract class!");
                
            entries[typeName] = new Entry(typeName, systemType);
        }
        catch(Exception ex)
        {
            string errPrefix = $"Unable to register an EntityType '{typeName}', reason: ";
            Logger.Get().Err(errPrefix + ex.Message);
        }
    }
    public void Override<TEntitySystem>(string typeName) where TEntitySystem : EntitySystem
    {
        typeName = typeName.ToLower();
        try
        {
            if(isFinalized)
                throw new Exception($"Registry has been already finalized!");

            if(string.IsNullOrWhiteSpace(typeName))
                throw new Exception($"Provided type name is null, empty or consists only of white-space characters!");

            if(!entries.ContainsKey(typeName))
                throw new Exception($"Entry with name '{typeName}' doesn't exists!");

            Type systemType = typeof(TEntitySystem);
            if(!systemType.IsClass)
                throw new Exception($"Provided EntitySystem '{systemType.Name}' type is not a class!");

            if(systemType.IsAbstract)
                throw new Exception($"Provided EntitySystem '{systemType.Name}' type is an abstract class!");

            entries[typeName] = new Entry(typeName, systemType);
        }
        catch(Exception ex)
        {
            string errPrefix = $"Unable to register an EntityType '{typeName}', reason: ";
            Logger.Get().Err(errPrefix + ex.Message);
        }
    }
    
    //TODO : implement global component methods
    public void GlobalComponent<TComponent>() where TComponent : struct
    {
        throw new NotImplementedException(nameof(GlobalComponent));
    }
    public void GlobalComponent<TComponent, TEntitySystem>() where TComponent : struct where TEntitySystem : EntitySystem
    {
        throw new NotImplementedException(nameof(GlobalComponent));
    }

    internal EntityType[] GetEntityTypes()
    {
        EntityType[] entityTypes = new EntityType[finalEntries.Length];
        for(int i = 0; i < finalEntries.Length; i++)
        {
            FinalEntry finalEntry = finalEntries[i];
            entityTypes[i] = new EntityType(finalEntry.TypeName, new Entity.TypeID((uint)i), finalEntry.EntitySystem, finalEntry.UpdateMethods, finalEntry.RenderMethod, finalEntry.ComponentContainers);
        }

        return entityTypes;
    }

    internal void FinalizeRegistry()
    {
        if(isFinalized)
            throw new InvalidOperationException("Cannot Finalize EntityTypeRegistry, reason: It has already been finalized!");

        IComponentContainer[] GetComponentContainers(Type[] componentTypes)
        {
            List<IComponentContainer> componentContainers = new List<IComponentContainer>(componentTypes.Length);
            Dictionary<Type, int> componentIndexes = new Dictionary<Type, int>();

            var componentIndex = 0;
            foreach(Type componentType in componentTypes)
            {
                if(!componentType.IsValueType)
                {
                    Logger.Get().Err($"Unable to create component of type '{componentType.Name}', reason: Provided type is not a ValueType!");
                    continue;
                }

                Type closedType = typeof(ComponentContainers<>).MakeGenericType(componentType);
                IComponentContainer componentContainer = (IComponentContainer)Activator.CreateInstance(closedType);

                componentIndexes.Add(componentType, componentIndex);
                componentContainers.Add(componentContainer);
                componentIndex++;
            }

            return componentContainers.ToArray();
        }
        List<EntityType.Method> GetUpdateMethods(EntitySystem entitySystem, Dictionary<Type, IComponentContainer> componentContainerMap)
        {
            var methodsInfoList = new List<MethodInfo>();
            var seenVirtualSlots = new HashSet<MethodInfo>();

            Type current = entitySystem.GetType();
            Type baseEntitySystemType = typeof(EntitySystem);
            while(current != null && current != baseEntitySystemType)
            {
                var methodInfos = current.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly)
                    .Where(m => m.Name == "Update");

                foreach(var method in methodInfos)
                {
                    if(method.IsVirtual)
                    {
                        var baseDef = method.GetBaseDefinition();
                        if (seenVirtualSlots.Contains(baseDef))
                            continue;
                        
                        seenVirtualSlots.Add(baseDef);
                    }

                    methodsInfoList.Add(method);
                }

                current = current.BaseType;
            }

            List<EntityType.Method> updateMethods = null;

            for(int m = 0; m < methodsInfoList.Count; m++)
            {
                try
                {
                    EntityType.Method method = new EntityType.Method(methodsInfoList[m], entitySystem, componentContainerMap);
                    updateMethods ??= new List<EntityType.Method>(methodsInfoList.Count);
                    updateMethods.Add(method);
                }
                catch(Exception ex)
                {
                    var paramList = string.Join(", ", methodsInfoList[m].GetParameters()
                        .Select(p => $"{(p.ParameterType.IsByRef ? "ref " : "")}{(p.ParameterType.GetElementType() ?? p.ParameterType).Name} {p.Name}"));

                    Logger.Get().Err($"Failed to bind method '{methodsInfoList[m].DeclaringType?.Name}.{methodsInfoList[m].Name}({paramList})': {ex.Message}");
                }
            }

            return updateMethods;
        }
        EntityType.Method GetRenderMethod(EntitySystem entitySystem, Dictionary<Type, IComponentContainer> componentContainerMap)
        {
            Type current = entitySystem.GetType();
            Type baseEntitySystemType = typeof(EntitySystem);

            while(current != null && current != baseEntitySystemType)
            {
                var methods = current.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly)
                    .Where(m => m.Name == "Render").ToArray();

                if(methods.Length > 1)
                    throw new Exception("Multiple Render methods.");

                if(methods.Length == 1)
                {
                    var methodInfo = methods[0];
                    try
                    {
                        return new EntityType.Method(methodInfo, entitySystem, componentContainerMap);
                    }
                    catch(Exception ex)
                    {
                        Logger.Get().Err($"Failed to bind render method '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}': {ex.Message}");
                    }
                }

                current = current.BaseType;
            }
            return null;
        }

        List<FinalEntry> finalizedEntries = new List<FinalEntry>(entries.Count);

        uint entryIndex = 0;
        foreach(Entry entry in entries.Values)
        {
            var entityTypeName = entry.TypeName;
            var entityTypeID = new Entity.TypeID(entryIndex);

            EntitySystem entitySystem = (EntitySystem)Activator.CreateInstance(entry.EntitySystemType, entityTypeName, entityTypeID);
            Type[] componentTypes = entitySystem.GetComponentTypes();

            IComponentContainer[] componentContainersArray = GetComponentContainers(componentTypes);

            List<EntityType.Method> updateMethods = null;
            EntityType.Method renderMethod = null;

            Dictionary<Type, IComponentContainer> componentContainerMap = new Dictionary<Type, IComponentContainer>();
            for(int i = 0; i < componentContainersArray.Length; i++)
            {
                componentContainerMap ??= [];
                componentContainerMap.Add(componentContainersArray[i].ComponentType, componentContainersArray[i]);
            }
            
            try
            {
                updateMethods = GetUpdateMethods(entitySystem, componentContainerMap);
            }
            catch(Exception ex)
            {
                Logger.Get().Err($"Failed to get Update methods of {entitySystem.GetType().Name}, reason: {ex.Message}");
            }

            try
            {
                renderMethod = GetRenderMethod(entitySystem, componentContainerMap);
            }
            catch(Exception ex)
            {
                Logger.Get().Err($"Failed to get Render method of {entitySystem.GetType().Name}, reason: {ex.Message}");
            }

            finalizedEntries.Add(new FinalEntry(entityTypeName, entityTypeID, entitySystem, updateMethods.ToArray(), renderMethod, componentContainersArray));
            entryIndex++;
        }
        
        this.finalEntries = finalizedEntries.ToArray();
    }
}