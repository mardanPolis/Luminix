using System;

namespace Verse.Identifiers;

public readonly record struct Variant : IEquatable<Variant>
{
    private readonly ushort _value;

    public Variant(ushort value)
    {
        _value = value;
    }

    public ushort Value => _value;

    public bool Equals(Variant other) => _value == other._value;

    public override int GetHashCode() => _value;

    public override string ToString() => $"[Variant:{_value}]";
}