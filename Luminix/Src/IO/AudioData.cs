using System;

namespace Luminix.IO;

internal readonly struct AudioData()
{
    public byte[] PcmData { get; init; } = Array.Empty<byte>();
    public AudioClip.Format Format { get; init; }
    public AudioClip.SampleRate SampleRate { get; init; }
}