using System;

namespace Luminix;

public readonly record struct ID : IEquatable<ID>
{
    private readonly uint value;

    public ID(uint value)
    {
        this.value = value;
    }
    public static explicit operator ID(uint value) => new ID(value);
    public static explicit operator ID(long value) => new ID((uint)value);
    public static explicit operator ID(ulong value) => new ID((uint)value);

    public static implicit operator uint(ID id) => id.Value;
    public static implicit operator int(ID id) => (int)id.Value;
    public static implicit operator long(ID id) => id.Value;
    public static implicit operator ulong(ID id) => id.Value;

    public uint Value => value;

    public bool Equals(ID other) => value == other.value;

    public override int GetHashCode() => (int)value;

    public override string ToString() => $"[ID:{value}]";
}