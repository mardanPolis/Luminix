using System;

namespace Verse.Identifiers;

public readonly struct ID : IEquatable<ID>
{
    private readonly uint _value;

    public ID(uint value)
    {
        _value = value;
    }

    public uint Value => _value;

    public static bool operator ==(ID a, ID b) => a._value == b._value;
    public static bool operator !=(ID a, ID b) => a._value != b._value;

    public bool Equals(ID other) => _value == other._value;

    public override bool Equals(object obj) => obj is ID other && Equals(other);

    public override int GetHashCode() => (int)_value;

    public override string ToString() => $"[ID:{_value}]";
}