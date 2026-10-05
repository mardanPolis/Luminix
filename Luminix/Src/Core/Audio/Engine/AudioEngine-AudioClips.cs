using System;
using Luminix.Collections;

namespace Luminix;

internal sealed partial class AudioEngine : IAudioEngine, IDisposable
{
    private DenseMap<(LargeHash, AudioClip.Format, AudioClip.SampleRate), AudioClip> audioClips;

    private void InitializeAudioClips()
    {
        audioClips = new(InitialCapacity);
    }

    public AudioClip GetOrCreateClip(ReadOnlySpan<byte> pcmData, AudioClip.Format audioFormat, AudioClip.SampleRate sampleRate)
    {
        ValidateDevice();
        
        if(audioFormat == AudioClip.Format.Unknown) 
            throw new ArgumentException("Cant get or create a audio clip, reason: The provided audioFormat is unknown.");

        if(sampleRate == AudioClip.SampleRate.Invalid) 
            throw new ArgumentException("Cant get or create a audio clip, reason: The provided sample rate is invalid.");

        LargeHash largeHash = LargeHash.GetHash(pcmData);
        if(!audioClips.TryGetItem((largeHash, audioFormat, sampleRate), out var clip))
        {
            clip = CreateAudioClip(pcmData, audioFormat, sampleRate);
        }

        return clip;
    }
    private AudioClip CreateAudioClip(ReadOnlySpan<byte> pcmData, AudioClip.Format audioFormat, AudioClip.SampleRate sampleRate)
    {
        (AudioClip.SampleFormat sampleFormat, AudioClip.Channels channels, int sampleSize) = ExtractData(audioFormat);

        ulong frameCount = (ulong)pcmData.Length / (ulong)(sampleSize * (int)channels);
        float duration = frameCount / (float)(int)sampleRate;

        ThrowIfFailed(audioDevice.AudioClipNew(pcmData, frameCount, channels, sampleFormat, sampleRate, out IntPtr pClip));
        AudioClip audioClip = new AudioClip(pClip, duration, audioFormat, sampleRate);

        LargeHash largeHash = LargeHash.GetHash(pcmData);
        audioClips.Add((largeHash, audioFormat, sampleRate), audioClip);
        return audioClip;
    }

    public void DisposeClip(AudioClip audio)
    {
        ValidateDevice();

        if (!audio.IsDisposed)
        {
            audioDevice.AudioClipDispose(audio.ClipPointer);
            audio.Dispose();
            audioClips.Remove(audio);
        }
    }
    public void DisposeAllAudioClips()
    {
        foreach (var clip in audioClips.RightItems)
        {
            if (clip.IsDisposed)
                continue;

            audioDevice.AudioClipDispose(clip.ClipPointer);
            clip.Dispose();
        }

        audioClips.Clear();
    }
}