using System;
using Microsoft.Xna.Framework;

namespace Luminix;

internal struct AudioSourceState : IEquatable<AudioSourceState>
{
    internal AudioSourceState(IntPtr pointer, float duration)
    {
        Pointer = pointer;
        Version = 0;
        State = 0;
        IsSpatial = false;
        IsLooping = false;
        Volume = 1.0f;
        Pitch = 1.0f;
        Pan = 0.0f;
        Position = Vector3.Zero;
        AttenuationModel = 0;
        Rolloff = 1.0f;
        MinDistance = 1.0f;
        MaxDistance = 100.0f;
        Duration = duration;
        Cursor = 0.0f;
    }

    internal IntPtr Pointer;
    internal Vector3 Position;

    internal volatile int Version; // (version & 1) == 0 means a write is not in progress
    internal int State;   // 1 Playing; 0 Stopped
    public int Generation;
    internal int AttenuationModel;
    internal float Volume;
    internal float Pitch;
    internal float Pan;
    internal float Rolloff;
    internal float MinDistance;
    internal float MaxDistance;
    internal float Duration;
    internal float Cursor;

    internal bool IsSpatial;
    internal bool IsLooping;
    internal Vector3 Velocity;

    public readonly bool Equals(AudioSourceState other)
    {
        return other.Pointer == Pointer;
    }

    public override readonly int GetHashCode()
    {
        return Pointer.GetHashCode();
    }
}