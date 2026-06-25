using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Luminix;

public sealed class EntityTypeRegistry : Workspace.Service
{
    private record struct Entry(string TypeName, Type EntitySystemType);
    private record struct FinalEntry(string TypeName, EntitySystem EntitySystem, EntityType.UpdateMethod[] UpdateMethods, IComponentContainer[] ComponentContainers);

    private Dictionary<string, Entry> entries = new();
    private FinalEntry[] finalEntries = null;
    private bool isFinalized;

    public void Register<TEntitySystem>(string typeName) where TEntitySystem : EntitySystem
    {
        try
        {
            if(isFinalized)
                throw new Exception($"Registry has been already finalized!");

            if(string.IsNullOrWhiteSpace(typeName))
                throw new Exception($"Provided type name is null, empty or consists only of white-space characters!");

            if(entries.ContainsKey(typeName))
                throw new Exception($"Entry with the same name already exists!");

            Type systemType = ValidateEntitySystem<TEntitySystem>();
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
        try
        {
            if(isFinalized)
                throw new Exception($"Registry has been already finalized!");

            if(string.IsNullOrWhiteSpace(typeName))
                throw new Exception($"Provided type name is null, empty or consists only of white-space characters!");

            if(!entries.ContainsKey(typeName))
                throw new Exception($"Entry with name '{typeName}' doesn't exists!");

            Type systemType = ValidateEntitySystem<TEntitySystem>();
            entries[typeName] = new Entry(typeName, systemType);
        }
        catch(Exception ex)
        {
            string errPrefix = $"Unable to register an EntityType '{typeName}', reason: ";
            Logger.Get().Err(errPrefix + ex.Message);
        }
    }

    private Type ValidateEntitySystem<TEntitySystem>()
    {
        Type systemType = typeof(TEntitySystem);
        if(!systemType.IsClass)
            throw new Exception($"Provided EntitySystem '{systemType.Name}' type is not a class!");

        if(systemType.IsAbstract)
            throw new Exception($"Provided EntitySystem '{systemType.Name}' type is an abstract class!");

        return systemType;
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

    internal void FinalizeRegistry()
    {
        finalEntries = new FinalEntry[entries.Count];

        var entryIndex = 0;
        foreach(Entry entry in entries.Values)
        {
            EntitySystem entitySystem = (EntitySystem)Activator.CreateInstance(entry.EntitySystemType);
            Type[] componentTypes = entitySystem.GetComponentTypes();

            MethodInfo[] methods = GetMethods(entitySystem.GetType());
            IComponentContainer[] componentContainersArray = GetComponentContainers(componentTypes);
            EntityType.UpdateMethod[] updateMethods = GetUpdateMethods(methods, entitySystem, componentContainersArray);

            finalEntries[entryIndex] = new FinalEntry(entry.TypeName, entitySystem, updateMethods, componentContainersArray);
            entryIndex++;
        }
        
        isFinalized = true;
    }

    internal EntityType[] GetEntityTypes()
    {
        EntityType[] entityTypes = new EntityType[finalEntries.Length];
        for(int i = 0; i < finalEntries.Length; i++)
        {
            FinalEntry finalEntry = finalEntries[i];
            entityTypes[i] = new EntityType(finalEntry.TypeName, new Entity.TypeID((uint)i), finalEntry.EntitySystem, finalEntry.UpdateMethods, finalEntry.ComponentContainers);
        }

        return entityTypes;
    }

    private IComponentContainer[] GetComponentContainers(Type[] componentTypes)
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
    private MethodInfo[] GetMethods(Type systemType)
    {
        var result = new List<MethodInfo>();
        var seenVirtualSlots = new HashSet<MethodInfo>();

        Type current = systemType;
        Type baseEntitySystemType = typeof(EntitySystem);
        while(current != null && current != baseEntitySystemType)
        {
            var methods = current.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly)
                .Where(m => m.Name == "ForEach");

            foreach(var method in methods)
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
    private EntityType.UpdateMethod[] GetUpdateMethods(MethodInfo[] methods, EntitySystem entitySystem, IComponentContainer[] componentContainers)
    {
        List<EntityType.UpdateMethod> updateMethods = [];

        for(int m = 0; m < methods.Length; m++)
        {
            try
            {
                updateMethods.Add(new EntityType.UpdateMethod(methods[m], entitySystem, componentContainers));
            }
            catch(Exception ex)
            {
                var paramList = string.Join(", ", methods[m].GetParameters()
                    .Select(p => $"{(p.ParameterType.IsByRef ? "ref " : "")}{(p.ParameterType.GetElementType() ?? p.ParameterType).Name} {p.Name}"));

                Logger.Get().Err($"Failed to bind method '{methods[m].DeclaringType?.Name}.{methods[m].Name}({paramList})': {ex.Message}");
            }
        }

        return updateMethods.ToArray();
    }

    
}