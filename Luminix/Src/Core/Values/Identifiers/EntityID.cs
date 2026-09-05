using System;

namespace Luminix;

public readonly record struct EntityID : IEquatable<EntityID>
{
    private readonly uint value;

    public EntityID(uint value)
    {
        this.value = value;
    }
    public static explicit operator EntityID(uint value) => new EntityID(value);
    public static explicit operator EntityID(long value) => new EntityID((uint)value);
    public static explicit operator EntityID(ulong value) => new EntityID((uint)value);

    public static implicit operator uint(EntityID id) => id.Value;
    public static implicit operator int(EntityID id) => (int)id.Value;
    public static implicit operator long(EntityID id) => id.Value;
    public static implicit operator ulong(EntityID id) => id.Value;

    public uint Value => value;

    public bool Equals(EntityID other) => value == other.value;

    public override int GetHashCode() => (int)value;

    public override string ToString() => $"[EntityID:{value}]";
}