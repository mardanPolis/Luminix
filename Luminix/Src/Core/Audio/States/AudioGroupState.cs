using System;

namespace Luminix;

internal struct AudioGroupState : IEquatable<AudioGroupState>
{
    internal IntPtr Pointer;
    internal string Name;
    internal volatile int Version; // (version & 1) == 0 means a write is not in progress
    internal float Volume;

    public readonly bool Equals(AudioGroupState other)
    {
        return other.Pointer == Pointer;
    }
}