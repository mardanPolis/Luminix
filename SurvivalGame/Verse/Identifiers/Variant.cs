using System;

namespace Verse.Identifiers;

public readonly struct Variant : IEquatable<Variant>
{
    private readonly ushort _value;

    public Variant(ushort value)
    {
        _value = value;
    }

    public ushort Value => _value;

    public static bool operator ==(Variant a, Variant b) => a._value == b._value;
    public static bool operator !=(Variant a, Variant b) => a._value != b._value;

    public bool Equals(Variant other) => _value == other._value;

    public override bool Equals(object obj) => obj is Variant other && Equals(other);

    public override int GetHashCode() => _value;

    public override string ToString() => $"[Variant:{_value}]";
}