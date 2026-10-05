using System;

namespace Luminix;

public readonly record struct AudioSource : IEquatable<AudioSource>
{
    internal AudioSource(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }
    internal int Index {get;}
    internal int Generation { get; }
    public override string ToString() => $"AudioSource({Index})";

}
