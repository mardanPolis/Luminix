using Microsoft.Xna.Framework;

namespace Luminix;

public readonly ref struct AudioSourceInfo
{
    internal AudioSourceInfo(int state, bool spatial, bool isLopped, float volume, float pitch, float pan, Vector3 position)
    {
        this.state = state;
        this.IsSpatial = spatial;
        this.IsLooped = isLopped;
        this.Volume = volume;
        this.Pitch = pitch;
        this.Pan = pan;
        this.Position = position;
    }

    private readonly int state { get; }
    public bool IsPlaying => state == 1;

    public readonly bool IsSpatial { get; }
    public readonly bool IsLooped { get; }
    public readonly float Volume { get; }
    public readonly float Pitch { get; }
    public readonly float Pan { get; }
    public readonly Vector3 Position { get; }
}