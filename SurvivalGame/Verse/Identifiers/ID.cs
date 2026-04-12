using System;

namespace Verse.Identifiers;

public readonly record struct ID : IEquatable<ID>
{
    private readonly uint value;

    public ID(uint value)
    {
        this.value = value;
    }

    public uint Value => value;

    public bool Equals(ID other) => value == other.value;

    public override int GetHashCode() => (int)value;

    public override string ToString() => $"[ID:{value}]";
}