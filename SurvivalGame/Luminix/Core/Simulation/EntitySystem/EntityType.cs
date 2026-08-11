using System;
using System.Collections.Generic;
using Luminix.Collections;
using Luminix.Extensions;
using Luminix.RegistrySystem;

namespace Luminix;

public sealed class EntityType : EntryItem<EntityType>
{
    internal EntityType(Registry<EntityType>.Token token, string id) : base(token, id)
    {
        // if (!typeof(EntitySystem).IsAssignableFrom(entitySystemType) || entitySystemType == typeof(EntitySystem))
        // {
        //     throw new ArgumentException(
        //         $"The specified type '{entitySystemType.FullName}' must derive from '{nameof(EntitySystem)}' and cannot be '{nameof(EntitySystem)}' itself.", 
        //         nameof(entitySystemType));
        // }

        // if (entitySystemType.IsAbstract)
        // {
        //     throw new ArgumentException(
        //         $"The specified system type '{entitySystemType.FullName}' is abstract and cannot be instantiated.", 
        //         nameof(entitySystemType));
        // }

        // EntitySystem = (EntitySystem)Activator.CreateInstance(entitySystemType);
        // EntitySystem.Initialize(this);
    }

    public Type EntitySystemType { get; }

}