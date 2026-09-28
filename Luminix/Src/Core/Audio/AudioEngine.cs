using System;
using System.Collections.Generic;
using System.Threading;
using Luminix.Collections;
using Luminix.Native.Audio;
using Microsoft.Xna.Framework;

namespace Luminix;

internal unsafe sealed class AudioEngine : IDisposable
{

    private static (AudioClip.SampleFormat, AudioClip.Channels, int) ExtractData(AudioClip.Format audioFormat)
    {
        return audioFormat switch
        {
            AudioClip.Format.Mono8     => (AudioClip.SampleFormat.U8, AudioClip.Channels.Mono, 1),
            AudioClip.Format.Stereo8   => (AudioClip.SampleFormat.U8, AudioClip.Channels.Stereo, 1),
            AudioClip.Format.Mono16    => (AudioClip.SampleFormat.S16, AudioClip.Channels.Mono, 2),
            AudioClip.Format.Stereo16  => (AudioClip.SampleFormat.S16, AudioClip.Channels.Stereo, 2),
            AudioClip.Format.Mono24    => (AudioClip.SampleFormat.S24, AudioClip.Channels.Mono, 3),
            AudioClip.Format.Stereo24  => (AudioClip.SampleFormat.S24, AudioClip.Channels.Stereo, 3),
            AudioClip.Format.MonoS32   => (AudioClip.SampleFormat.S32, AudioClip.Channels.Mono, 4),
            AudioClip.Format.StereoS32 => (AudioClip.SampleFormat.S32, AudioClip.Channels.Stereo, 4),
            AudioClip.Format.MonoF32   => (AudioClip.SampleFormat.F32, AudioClip.Channels.Mono, 4),
            AudioClip.Format.StereoF32 => (AudioClip.SampleFormat.F32, AudioClip.Channels.Stereo, 4),

            _ => throw new NotSupportedException($"Unsupported format: {audioFormat}")
        };
    }

    public AudioEngine()
    {
        audioDevice = AudioDevice.Create();
        audioDevice.Start();
        audioDevice.MasterSetVolume(1.0f);
        freeIndices = new(512);
        audioSourceStates = new AudioSourceState[1024];
        audioClips = new(128);
    }

    private AudioDevice audioDevice;
    private Stack<uint> freeIndices;
    private AudioSourceState[] audioSourceStates;
    private uint audioSourceCount;
    private DenseMap<(LargeHash, AudioClip.Format, AudioClip.SampleRate), AudioClip> audioClips;

    public void Dispose()
    {
        audioDevice.Dispose();
        freeIndices = null;
        audioSourceStates = null;
        audioClips = null;
    }

    public AudioClip GetOrCreateAudioClip(ReadOnlySpan<byte> pcmData, AudioClip.Format audioFormat, AudioClip.SampleRate sampleRate)
    {
        if(sampleRate == AudioClip.SampleRate.Invalid) 
            throw new ArgumentException("Cant get or create a audio clip, since the provided sample rate is invalid.");

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

        var pClip = audioDevice.AudioClipNew(pcmData, frameCount, channels, sampleFormat, sampleRate);
        AudioClip audioClip = new AudioClip(pClip, duration, audioFormat, sampleRate);

        LargeHash largeHash = LargeHash.GetHash(pcmData);
        audioClips.Add((largeHash, audioFormat, sampleRate), audioClip);
        return null;
    }

    public void DisposeAudioClip(AudioClip audio)
    {
        if (!audio.IsDisposed)
        {
            //AL.DeleteBuffer(audio.BufferID);
            audio.Dispose();
            audioClips.RemoveRight(audio);
        }
    }

    public void SetListenerPosition(float x, float y, float z)
    {
        audioDevice.ListenerSetPosition(x, y, z);
    }
    public void SetListenerVelocity(float x, float y, float z)
    {
        audioDevice.ListenerSetPosition(x, y, z);
    }
    public void SetListenerDirection(float x, float y, float z)
    {
        audioDevice.ListenerSetPosition(x, y, z);
    }
    public void SetListenerWorldUp(float x, float y, float z)
    {
        audioDevice.ListenerSetPosition(x, y, z);
    }

    //AudioSource Instance
    public AudioSource CreateAudioSource(AudioClip audio)
    {
        if(audio.IsDisposed)
            throw new ArgumentException("Cannot create AudioSource instance from audio, reason: Audio is disposed.");
        
        IntPtr source = 0;
        uint index;
        if (freeIndices.TryPop(out var result))
        {
            index = result;
            source = audioSourceStates[index].SourceID;
            // if (source == 0)
            //     source = AL.GenSource();
        }
        else
        {
            audioSourceCount++;
            //source = AL.GenSource();
            index = audioSourceCount;

            if (audioSourceStates.Length <= audioSourceCount)
                Array.Resize(ref audioSourceStates, audioSourceStates.Length * 2);
        }

        //AL.SetSourceProperty(source, SourceInteger.Buffer, (int)audio.BufferID);

        var handle = new AudioSource(index);
        var state = new AudioSourceState
        {
            SourceID = source,
            Version = 0,
            State = 2, // Stopped
            IsSpatial = false,
            Looped = false,
            Volume = 1.0f,
            Pitch = 1.0f,
            Pan = 0.0f,
            Position = Vector2.Zero
        };

        audioSourceStates[index] = state;
        return handle;
    }

    public AudioSource CreateAudioSource(AudioResource audio)
    {
        if(audio.IsDisposed)
            throw new ArgumentException($"Cannot create AudioSource instance from {audio}, reason: Audio is disposed.");

        return CreateAudioSource(audio.Clip);
    }

    public void DestroyAudioSource(AudioSource handle)
    {
        
    }

    public void DisposeAudioSource(AudioSource handle)
    {
        
    }

    public void Play(AudioSource handle)
    {
        
    }

    public void Stop(AudioSource handle)
    {
        
    }

    public void Resume(AudioSource handle)
    {
        
    }
    public void Pause(AudioSource handle)
    {
        
    }
    public void SetSpatial(AudioSource handle, bool spatial)
    {
        
    }
    public void SetLooping(AudioSource handle, bool loop)
    {
        
    }
    public void SetVolume(AudioSource handle, float volume)
    {
        
    }
    public void SetPitch(AudioSource handle, float pitch)
    {
        
    }
    public void SetPan(AudioSource handle, float pan)
    {
        
    }
    public void SetPosition(AudioSource handle, Vector2 position)
    {
        
    }

    private void CloseState(ref AudioSourceState sourceState)
    {
        Interlocked.Increment(ref sourceState.Version);
    }
    private void OpenState(ref AudioSourceState sourceState)
    {
        Interlocked.Decrement(ref sourceState.Version);
    }

    // public bool TryGetInfo(AudioSource handle, out AudioSourceInfo audioSourceInfo)
    // {
        
    // }
}