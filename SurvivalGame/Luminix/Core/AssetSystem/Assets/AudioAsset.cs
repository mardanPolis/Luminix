using System;
using Microsoft.Xna.Framework.Audio;

namespace Luminix;

public sealed class AudioAsset : Asset
{
    internal AudioAsset(string identifier, SoundEffect soundEffect) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(soundEffect, nameof(soundEffect));

        SoundEffect = soundEffect;
    }

    public SoundEffect SoundEffect { get; }
}
