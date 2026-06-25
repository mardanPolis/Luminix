using Microsoft.Xna.Framework;
using Luminix.Identifiers;
using System;

namespace Luminix;

public struct Entity
{
    public readonly record struct TypeID : IEquatable<TypeID>
    {
        private readonly uint value;

        public TypeID(uint value)
        {
            this.value = value;
        }

        public uint Value => value;

        public bool Equals(TypeID other) => value == other.value;

        public override int GetHashCode() => (int)value;

        public override string ToString() => $"[Type:{value}]";
    }

    internal Entity(ID iD)
    {
        ID = iD;
    }

    public ID ID { get; }
    public bool Enabled { get; set; }

    private static World world;

    internal static void SetWorld(World world)
    {
        Entity.world = world;
    }

    internal static void RemWorld()
    {
        Entity.world = null;
    }

    public static bool HasComponent<T>(ID entityID) where T : struct
    {
        return world.EntityHasComponent<T>(entityID);
    }

    public static ref T GetComponent<T>(ID entityID) where T : struct
    {
        return ref world.GetEntityComponent<T>(entityID);
    }

    public static ID Create(Entity.TypeID type)
    {
        return world.CreateEntity(type);
    }

    public static void Destroy(ID entityID)
    {
        world.DestroyEntity(entityID);
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
    public override string ToString()
    {
        return $"Entity{ID}";
    }
}
