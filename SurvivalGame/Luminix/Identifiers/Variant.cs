using System;

namespace Luminix.Identifiers;

public readonly record struct Variant : IEquatable<Variant>
{
    private readonly ushort value;

    public Variant(ushort value)
    {
        this.value = value;
    }

    public ushort Value => value;

    public bool Equals(Variant other) => value == other.value;

    public override int GetHashCode() => value;

    public override string ToString() => $"[Variant:{value}]";
}