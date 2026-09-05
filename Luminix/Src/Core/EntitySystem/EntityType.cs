using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Luminix.Collections;

namespace Luminix;

public sealed class EntityType : Def
{
    internal EntityType()
    {
        componentTypes = [];
    }

    private Dictionary<Type, IComponentType> componentTypes;

    [JsonInclude]
    private EntitySystem entitySystem { get; set; }

    [JsonInclude]
    private IComponentType[] components
    {
        set
        {
            componentTypes.Clear();
            for(int i = 0; i < value.Length; i++)
            {
                if(value[i].Type == typeof(EntityID) || value[i].Type == typeof(Enabled))
                    continue;

                componentTypes.Add(value[i].Type, value[i]);
            }
        }
    }

    public EntitySystem GetEntitySystem() => entitySystem;

    public bool HasComponent<T>() where T : struct
    {
        return componentTypes.ContainsKey(typeof(T));
    }

    public void SetComponentDefault<T>(T value) where T : struct
    {
        if(!componentTypes.TryGetValue(typeof(T), out IComponentType componentType))
            throw new KeyNotFoundException();
        
        ((ComponentType<T>)componentType).Default = value;
    }
    public T GetComponentDefault<T>() where T : struct
    {
        if(componentTypes.TryGetValue(typeof(T), out IComponentType componentType))
            return ((ComponentType<T>)componentType).Default;
        
        throw new KeyNotFoundException();
    }

    public bool TryGetComponentDefault<T>(out T value) where T : struct
    {
        if(componentTypes.TryGetValue(typeof(T), out IComponentType componentType))
        {
            value = ((ComponentType<T>)componentType).Default;
            return true;
        }

        value = default;
        return false;
    }

    internal IComponentContainer[] GetComponentContainers()
    {
        List<IComponentContainer> containers = new List<IComponentContainer>(componentTypes.Count + 2)
        {
            ComponentType<EntityID>.NewDefault().GetComponentContainer(),
            ComponentType<Enabled>.NewDefault().GetComponentContainer()
        };
        
        foreach(var compType in componentTypes)
        {
            if(compType.Key == typeof(EntityID) || compType.Key == typeof(Enabled))
                continue;
            
            containers.Add(compType.Value.GetComponentContainer());
        }

        return containers.ToArray();
    }
}