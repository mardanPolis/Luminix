using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Luminix;

public class EntityType : Def
{
    [JsonConstructor] internal EntityType()
    {
        componentTypes = [];
    }


    [SetsRequiredMembers] public EntityType(string name, Type entitySystemType, params ReadOnlySpan<(Type Type, object value)> components)
    {
        Name = name;

        if (entitySystemType is not null)
        {
            if (!entitySystemType.IsClass)
                throw new ArgumentException($"Provided entity system type '{entitySystemType}' is not a class type.", nameof(entitySystemType));

            if (!typeof(EntitySystem).IsAssignableFrom(entitySystemType))
                throw new ArgumentException($"Provided entity system type '{entitySystemType}' does not inherit from type '{typeof(EntitySystem)}'.", nameof(entitySystemType));

            if (entitySystemType.IsAbstract || entitySystemType.IsInterface)
                throw new ArgumentException($"Provided entity system type '{entitySystemType}' is abstract or is an interface.", nameof(entitySystemType));

            entitySystem = (EntitySystem)Activator.CreateInstance(entitySystemType);
        }

        var componentTypes = new List<IComponentType>();

        var seenTypes = new HashSet<Type>();

        foreach (var component in components)
        {
            if (component.Type is null)
                throw new ArgumentException("Component type is null.", nameof(components));

            if (!seenTypes.Add(component.Type))
                throw new ArgumentException($"Duplicate component type '{component.Type}' was provided.", nameof(components));

            if (component.value is null)
            {
                if (component.Type.IsValueType && Nullable.GetUnderlyingType(component.Type) is null)
                    throw new ArgumentException($"Provided value is null but component type '{component.Type}' is a non-nullable value type.", nameof(components));
            }
            else if (!component.Type.IsInstanceOfType(component.value))
            {
                throw new ArgumentException($"Provided value of type '{component.value.GetType()}' is not assignable to component type '{component.Type}'.", nameof(components));
            }

            IComponentType componentType;
            try
            {
                componentType = (IComponentType)Activator.CreateInstance(TypeRegistry.GetClosedGenericType(typeof(ComponentType<>), component.Type));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create ComponentType instance for '{component.Type}'.", ex);
            }

            componentType.Default = component.value;
        }

        this.componentTypes = [];
        this.components = CollectionsMarshal.AsSpan(componentTypes);
    }

    private Dictionary<Type, IComponentType> componentTypes;
    private string[] tagArray;
    private HashSet<string> tagSet;

    [JsonInclude] private EntitySystem entitySystem { get; set; }
    [JsonInclude] private ReadOnlySpan<IComponentType> components
    {
        get => componentTypes.Values.ToArray();
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
    [JsonInclude] private string[] tags
    {
        get => tagArray;
        set
        {
            tagSet = new HashSet<string>(value);
            if(tagSet.Count != value.Length)
            {
                tagArray = tagSet.ToArray();
                return;
            }
            tagArray = value;
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

    public bool HasTag(string tag)
    {
        return tagSet.Contains(tag);
    }
    public ReadOnlySpan<string> GetTags()
    {
        return tagArray;
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

    internal Dictionary<Type, IComponentType> GetComponentTypes()
    {
        return componentTypes;
    }
}