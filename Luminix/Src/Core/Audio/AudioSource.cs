namespace Luminix;

public readonly record struct AudioSource
{
    internal AudioSource(uint index)
    {
        this.index = index;
    }

    private readonly uint index;
    internal uint Index => index;

    public readonly bool IsValid => index != 0;

    public bool Equals(AudioSource other) => index == other.index;

    public override int GetHashCode() => (int)index;

    public override string ToString() => IsValid ? $"[AudioSource:{index}]" : "[AudioSource:invalid]";

}
