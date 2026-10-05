using System.Threading;
using Microsoft.Xna.Framework;

namespace Luminix;

public readonly ref struct AudioSourceInfo
{
    internal AudioSourceInfo(in AudioSourceState state, bool threaded) 
    { 
        if(threaded)
        {
            while (true)
            {
                int v1 = state.Version;

                if ((v1 & 1) != 0) 
                {
                    Thread.SpinWait(1);
                    continue;
                }

                Position = state.Position;
                Velocity = state.Velocity;

                Volume = state.Volume;
                Pitch = state.Pitch;
                Pan = state.Pan;
                AttenuationModel = state.AttenuationModel;
                Rolloff = state.Rolloff;
                MinDistance = state.MinDistance;
                MaxDistance = state.MaxDistance;
                Length = state.Duration;
                Cursor = state.Cursor;

                IsPlaying = state.State == 1;
                IsSpatial = state.IsSpatial;
                IsLooping = state.IsLooping;
                
                Thread.MemoryBarrier();

                int v2 = state.Version;

                if (v1 == v2)
                    break;
            }

            return;
        }


        Position = state.Position;
        Velocity = state.Velocity;

        Volume = state.Volume;
        Pitch = state.Pitch;
        Pan = state.Pan;
        AttenuationModel = state.AttenuationModel;
        Rolloff = state.Rolloff;
        MinDistance = state.MinDistance;
        MaxDistance = state.MaxDistance;
        Length = state.Duration;
        Cursor = state.Cursor;

        IsPlaying = state.State == 1;
        IsSpatial = state.IsSpatial;
        IsLooping = state.IsLooping;
        
    }
    
    public Vector3 Position { get; init; }
    public Vector3 Velocity { get; init; }
    public int AttenuationModel { get; init; }
    
    public float Volume { get; init; }
    public float Pitch { get; init; }
    public float Pan { get; init; }
    public float Rolloff { get; init; }
    public float MinDistance { get; init; }
    public float MaxDistance { get; init; }
    public float Length { get; init; }
    public float Cursor { get; init; }

    public bool IsPlaying { get; }
    public bool IsSpatial { get; init; }
    public bool IsLooping { get; init; }

}