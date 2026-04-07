using System;

namespace Verse.Identifiers;

public readonly record struct ID : IEquatable<ID>
{
    private readonly uint _value;

    public ID(uint value)
    {
        _value = value;
    }

    public uint Value => _value;

    public bool Equals(ID other) => _value == other._value;

    public override int GetHashCode() => (int)_value;

    public override string ToString() => $"[ID:{_value}]";
}