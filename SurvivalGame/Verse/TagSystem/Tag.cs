using System;

namespace Verse.TagSystem.Values;

public readonly struct Tag(uint value)
{
    private readonly uint value = value;
    
    public static bool operator ==(Tag a, Tag b) => a.value == b.value;
    public static bool operator !=(Tag a, Tag b) => a.value != b.value;

    public bool Equals(Tag other) => value == other.value;

    public override bool Equals(object obj) => obj is Tag other && Equals(other);
    public override int GetHashCode() => value.GetHashCode();

    public override string ToString() => $"[Tag:{value}]";
}
