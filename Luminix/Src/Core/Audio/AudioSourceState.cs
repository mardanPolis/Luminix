using System;
using Microsoft.Xna.Framework;

namespace Luminix;

internal struct AudioSourceState 
{
    internal IntPtr SourcePtr;
    internal int Version; // 0 means no write is in progress
    internal int State;   // 1 Playing, 0 Stopped
    
    internal bool IsSpatial;
    internal bool IsLooping;
    internal float Volume;
    internal float Pitch;
    internal float Pan;
    internal Vector3 Position;
    internal int AttenuationModel;
    internal float Rolloff;
    internal float MinDistance;
    internal float MaxDistance;
    internal float Length;
    internal float Cursor;

    public static explicit operator AudioSourceInfo(AudioSourceState state) => new(state.State, state.IsSpatial, state.IsLooping, state.Volume, state.Pitch, state.Pan, state.Position);
}