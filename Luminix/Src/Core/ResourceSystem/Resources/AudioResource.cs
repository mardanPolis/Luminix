using System;

namespace Luminix;

public sealed class AudioResource : Resource
{
    internal AudioResource(AudioClip clip, string name) 
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(clip, nameof(clip));

        this.clip = clip;
    }

    private AudioClip clip;
    public AudioClip Clip => clip;
    
    public sealed override string ToString()
    {
        string identifier = !string.IsNullOrEmpty(Identifier) ? Identifier : "Unset";
        return $"[AudioResource: '{identifier}'; {clip}]";
    }

    private protected override void OnDispose()
    {
        throw new NotImplementedException();
    }
}