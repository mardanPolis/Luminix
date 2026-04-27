namespace Luminix.Identifiers;

public readonly record struct Tag(uint value)
{
    private readonly uint value = value;
    
    public bool Equals(Tag other) => value == other.value;

    public override int GetHashCode() => value.GetHashCode();

    public override string ToString() => $"[Tag:{value}]";
}
