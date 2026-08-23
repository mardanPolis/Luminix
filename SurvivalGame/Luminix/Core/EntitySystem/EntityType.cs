using System;
using System.Text.Json.Nodes;

namespace Luminix;

public sealed class EntityType : Def
{
    internal EntityType(Defs.Token token, string name, JsonObject entitySystem, JsonObject components) : base(token, name)
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