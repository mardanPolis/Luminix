using System;
using Microsoft.Xna.Framework.Audio;

namespace Luminix;

public sealed class AudioAsset : Asset
{
    internal AudioAsset(string identifier, byte[] pcmData, AudioClip.Format audioFormat, AudioClip.SampleRate sampleRate) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(pcmData, nameof(pcmData));

        this.pcmData = pcmData;
        this.AudioFormat = audioFormat;
        this.SampleRate = sampleRate;
    }

    private byte[] pcmData;
    public ReadOnlySpan<byte> PCMData => pcmData;
    public AudioClip.Format AudioFormat { get; }
    public AudioClip.SampleRate SampleRate { get; }

    protected override void OnDispose()
    {
        pcmData = null;
    }
}
