using System;
using Microsoft.Xna.Framework;

namespace Luminix;

internal struct AudioSourceState 
{
    public IntPtr SourceID;
    public int Version; // 0 means no write is in progress
    public int State;   // 1 Playing, 0 Stopped
    
    public bool IsSpatial;
    public bool Looped;
    public float Volume;
    public float Pitch;
    public float Pan;
    public Vector2 Position;

    public static explicit operator AudioSourceInfo(AudioSourceState state) => new(state.State, state.IsSpatial, state.Looped, state.Volume, state.Pitch, state.Pan, state.Position);
}