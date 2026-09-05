using System;

namespace Luminix;

public struct Enabled : IEquatable<Enabled>
{
    private bool value;

    public Enabled(bool value)
    {
        this.value = value;
    }
    public static implicit operator Enabled(bool value) => new(value);
    public static implicit operator bool(Enabled value) => value;
    public bool Value => value;

    public bool Equals(Enabled other) => value == other.value;

    public override int GetHashCode() => value.GetHashCode();

    public override string ToString() => value ? "Enabled" : "Disabled";
}