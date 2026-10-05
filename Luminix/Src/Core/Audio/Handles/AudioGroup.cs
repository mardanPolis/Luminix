using System;

namespace Luminix;

public readonly record struct AudioGroup : IEquatable<AudioGroup>
{
    internal AudioGroup(int index)
    {
        Index = index;
    }
    internal int Index {get;}
    public override string ToString() => $"AudioGroup({Index})";
}