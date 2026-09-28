namespace Luminix;

public readonly record struct AudioSource
{
    internal AudioSource(int index)
    {
        this.index = index;
    }

    private readonly int index;
    internal int Index => index;

    public readonly bool IsValid => index != 0;

    public bool Equals(AudioSource other) => index == other.index;

    public override int GetHashCode() => index;

    public override string ToString() => IsValid ? $"[AudioSource:{index}]" : "[AudioSource:invalid]";

}
