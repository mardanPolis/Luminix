using System;

namespace Luminix;

public sealed class AudioClip
{
    public enum Format
    {
        Unknown         = 0,
        Mono8           = 1,
        Stereo8         = 2,
        Mono16          = 3,
        Stereo16        = 4,
        Mono24          = 5,
        Stereo24        = 6,
        MonoS32         = 7,
        StereoS32       = 8,
        MonoF32         = 9,
        StereoF32       = 10,
        StereoFloat32 = 11,
    }
    internal enum SampleFormat : uint
    {
        Unknown = 0,
        U8 = 1,
        S16 = 2,
        S24 = 3,
        S32 = 4,
        F32 = 5,
        Count
    }
    internal enum Channels : uint
    {
        Mono    = 1,
        Stereo  = 2
    }

    public enum SampleRate : uint
    {
        Invalid = 0,

        Hz8000   = 8000,
        Hz11025  = 11025,
        Hz16000  = 16000,
        Hz22050  = 22050,
        Hz24000  = 24000,
        Hz32000  = 32000,
        Hz44100  = 44100,
        Hz48000  = 48000,
        Hz88200  = 88200,
        Hz96000  = 96000,
        Hz176400 = 176400,
        Hz192000 = 192000
    }

    internal AudioClip(IntPtr pClip, float duration, Format audioFormat, SampleRate sampleRate)
    {
        this.pClip = pClip;
        this.duration = duration;
        this.audioFormat = audioFormat;
        this.sampleRate = sampleRate;
    }

    private IntPtr pClip;

    private float duration;
    private Format audioFormat;
    private SampleRate sampleRate;
    private bool isDisposed;

    internal IntPtr ClipPointer => pClip;

    public float Duration => duration;
    public Format ClipFormat => audioFormat;
    public SampleRate ClipSampleRate => sampleRate;
    public bool IsDisposed => isDisposed;

    public sealed override string ToString()
    {
        if (isDisposed)
            return "Disposed";

        double sampleRateKhz = (int)sampleRate / 1000.0;
        return $"s | {sampleRateKhz:0.0}kHz | {duration} sec";
    }

    internal void Dispose()
    {
        isDisposed = true;
    }
}