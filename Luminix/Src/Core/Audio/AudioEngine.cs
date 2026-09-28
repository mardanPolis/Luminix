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
    ~AudioEngine()
    {
        audioDevice.Stop();
        audioDevice.Dispose();
    }

    private AudioDevice audioDevice;
    private Stack<int> freeIndices;
    private int lastIndex;
    private int audioSourceCount;
    private DenseMap<(LargeHash, AudioClip.Format, AudioClip.SampleRate), AudioClip> audioClips;

    private AudioSourceState[] audioSourceStates;

    private float masterVolume = 1.0f;

    private Vector3 listenerPosition;
    private Vector3 listenerVelocity;
    private Vector3 listenerDirection;
    private Vector3 listenerWorldUp;

    private bool DeviceValid()
    {
        if (audioDevice.IsInvalid)
        {
            Logger.NewLog(Logger.Log.Level.Error, "AudioDevice is invalid.");
            return false;
        }

        return true;
    }
    public void Dispose()
    {
        audioDevice.Dispose();
        freeIndices = null;
        audioSourceStates = null;
        audioClips = null;
    }

    //Get Methods dont check the fence since they are called on the same thread, the fencing is only used on AudioBridge

    //Master
    public void SetMasterVolume(float volume)
    {
        if(DeviceValid())
            return;

        audioDevice.MasterSetVolume(volume);
        masterVolume = volume;
    }
    public float GetMasterVolume()
    {
        if(DeviceValid())
            return 0.0f;

        return masterVolume;
    }
    public AudioClip.SampleRate GetMasterSampleRate()
    {
        if(DeviceValid())
            return AudioClip.SampleRate.Hz8000;
            
        return (AudioClip.SampleRate)audioDevice.MasterGetSampleRate();
    }
    public AudioClip.Channels GetMasterChannels()
    {
        if(DeviceValid())
            return AudioClip.Channels.Stereo;
            
        return (AudioClip.Channels)audioDevice.MasterGetChannels();
    }

    //Listeners
    public void SetListenerPosition(float x, float y, float z)
    {
        if(DeviceValid())
            return;
            
        audioDevice.ListenerSetPosition(x, y, z);
        listenerPosition = new Vector3(x, y, z);
    }
    public void SetListenerVelocity(float x, float y, float z)
    {
        if(DeviceValid())
            return;
            
        audioDevice.ListenerSetVelocity(x, y, z);
        listenerPosition = new Vector3(x, y, z);
    }
    public void SetListenerDirection(float x, float y, float z)
    {
        if(DeviceValid())
            return;
            
        audioDevice.ListenerSetDirection(x, y, z);
        listenerDirection = new Vector3(x, y, z);
    }
    public void SetListenerWorldUp(float x, float y, float z)
    {
        if(DeviceValid())
            return;
            
        audioDevice.ListenerSetWorldUp(x, y, z);
        listenerWorldUp = new Vector3(x, y, z);
    }

    public Vector3 GetListenerPosition()
    {
        if (!DeviceValid())
            return Vector3.Zero;

        return listenerPosition;
    }
    public Vector3 GetListenerVelocity()
    {
        if (!DeviceValid())
            return Vector3.Zero;

        return listenerVelocity;
    }
    public Vector3 GetListenerDirection()
    {
        if (!DeviceValid())
            return Vector3.Zero;
            
        return listenerDirection;
    }
    public Vector3 GetListenerWorldUp()
    {
        if (!DeviceValid())
            return Vector3.Zero;

        return listenerWorldUp;
    }

    //AudioClips
    public AudioClip GetOrCreateAudioClip(AudioAsset audioAsset)
    {
        var pcmData = audioAsset.PCMData;
        var format = audioAsset.AudioFormat;
        var sampleRate = audioAsset.SampleRate;

        return GetOrCreateAudioClip(pcmData, format, sampleRate);
    }
    public AudioClip GetOrCreateAudioClip(ReadOnlySpan<byte> pcmData, AudioClip.Format audioFormat, AudioClip.SampleRate sampleRate)
    {
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
            audioDevice.AudioClipDispose(audio.ClipPointer);
            audio.Dispose();
            audioClips.RemoveRight(audio);
        }
    }

    //AudioSource
    public AudioSource CreateAudioSource(AudioClip audio)
    {
        ObjectDisposedException.ThrowIf(audioDevice.IsInvalid, audioDevice);

        if(audio.IsDisposed)
            throw new ArgumentException("Cannot create AudioSource instance from the provided AudioClip, reason: Audio is disposed.");
        
        if(audioDevice.AudioSourceNew(audio.ClipPointer, out IntPtr source) == AudioDeviceResult.Success)
            throw new InvalidOperationException("Failed to create AudioSource.");
            
        audioSourceCount++;

        if (!freeIndices.TryPop(out var index)) {
            lastIndex++;
            index = lastIndex;

            if (audioSourceStates.Length <= lastIndex)
                Array.Resize(ref audioSourceStates, audioSourceStates.Length * 2);
        }

        audioDevice.AudioSourceLength(source, out float length);
        var handle = new AudioSource(index);
        var state = new AudioSourceState
        {
            SourcePtr = source,
            Version = 0,
            State = 0,
            IsSpatial = false,
            IsLooping = false,
            Volume = 1.0f,
            Pitch = 1.0f,
            Pan = 0.0f,
            Position = Vector3.Zero,
            AttenuationModel = 0,
            Rolloff = 1.0f,
            MinDistance = 1.0f,
            MaxDistance = 100.0f,
            Length = length,
            Cursor = 0.0f
        };

        audioSourceStates[index] = state;
        return handle;
    }
    public AudioSource CreateAudioSource(AudioResource audio)
    {
        return CreateAudioSource(audio.Clip);
    }
    
    private bool AudioSourceValid(AudioSource handle)
    {
        if(!DeviceValid())
            return false;

        if (!handle.IsValid)
        {
            Logger.NewLog(Logger.Log.Level.Warning, $"AudioSource handle [Index: {handle.Index}] is invalid.");
            return false;
        }

        if (audioSourceStates[handle.Index].SourcePtr == 0)
        {
            Logger.NewLog(Logger.Log.Level.Warning, $"AudioSource at index {handle.Index} has a null pointer (disposed or uninitialized).");
            return false;
        }

        Logger.NewLog(Logger.Log.Level.Info, $"AudioSource handle [Index: {handle.Index}] successfully validated.");
        return true;
    }

    public void DestroyAudioSource(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceDispose(state.SourcePtr);

        OpenState(ref state);
        state.SourcePtr = 0; // invalidate source ptr
        CloseState(ref state);
    }

    public void Play(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourcePlay(state.SourcePtr);

        OpenState(ref state);
        state.State = 1;
        CloseState(ref state);
    }
    public void Stop(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceStop(state.SourcePtr);

        OpenState(ref state);
        state.State = 0;
        CloseState(ref state);
    }
    public void Resume(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceResume(state.SourcePtr);

        OpenState(ref state);
        state.State = 1;
        CloseState(ref state);
    }
    public void Pause(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourcePause(state.SourcePtr);

        OpenState(ref state);
        state.State = 0;
        CloseState(ref state);
    }

    public bool IsPlaying(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return false;

        return audioSourceStates[handle.Index].State == 1;
    }

    public void SetVolume(AudioSource handle, float volume)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetVolume(state.SourcePtr, volume);

        OpenState(ref state);
        state.Volume = volume;
        CloseState(ref state);
    }
    public void SetPitch(AudioSource handle, float pitch)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetPitch(state.SourcePtr, pitch);

        OpenState(ref state);
        state.Pitch = pitch;
        CloseState(ref state);
    }
    public void SetPan(AudioSource handle, float pan)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetPan(state.SourcePtr, pan);

        OpenState(ref state);
        state.Pan = pan;
        CloseState(ref state);
    }
    public float GetVolume(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0f;

        return audioSourceStates[handle.Index].Volume;
    }
    public float GetPitch(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0f;

        return audioSourceStates[handle.Index].Pitch;
    }
    public float GetPan(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0f;

        return audioSourceStates[handle.Index].Pan;
    }
    
    public void SetSpatial(AudioSource handle, bool spatial)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetSpatial(state.SourcePtr, spatial);

        OpenState(ref state);
        state.IsSpatial = spatial;
        CloseState(ref state);
    }
    public void SetLooping(AudioSource handle, bool loop)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetLooping(state.SourcePtr, loop);

        OpenState(ref state);
        state.IsLooping = loop;
        CloseState(ref state);
    }
    public bool IsSpatial(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return false;

        return audioSourceStates[handle.Index].IsSpatial;
    }
    public bool IsLooping(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return false;

        return audioSourceStates[handle.Index].IsLooping;
    }

    public void SetPosition(AudioSource handle, Vector3 position)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetPosition(state.SourcePtr, position.X, position.Y, position.Z);

        OpenState(ref state);
        state.Position = position;
        CloseState(ref state);
    }
    public Vector3 GetPosition(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return Vector3.Zero;

        return audioSourceStates[handle.Index].Position;
    }

    public void SetAttenuationModel(AudioSource handle, int attenuationModel)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetAttenuationModel(state.SourcePtr, attenuationModel);

        OpenState(ref state);
        state.AttenuationModel = attenuationModel;
        CloseState(ref state);
    }
    public int GetAttenuationModel(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0;

        return audioSourceStates[handle.Index].AttenuationModel;
    }

    public void SetRolloff(AudioSource handle, float rolloff)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetRolloff(state.SourcePtr, rolloff);

        OpenState(ref state);
        state.Rolloff = rolloff;
        CloseState(ref state);
    }
    public float GetRolloff(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0;

        return audioSourceStates[handle.Index].Rolloff;
    }

    public void SetMinDistance(AudioSource handle, float distance)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetMinDistance(state.SourcePtr, distance);

        OpenState(ref state);
        state.MinDistance = distance;
        CloseState(ref state);
    }
    public float GetMinDistance(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0;

        return audioSourceStates[handle.Index].MinDistance;
    }

    public void SetMaxDistance(AudioSource handle, float distance)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetMaxDistance(state.SourcePtr, distance);

        OpenState(ref state);
        state.MaxDistance = distance;
        CloseState(ref state);
    }
    public float GetMaxDistance(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0;

        return audioSourceStates[handle.Index].MaxDistance;
    }

    public void SetCursor(AudioSource handle, float cursor)
    {
        if (!AudioSourceValid(handle))
            return;

        ref var state = ref audioSourceStates[handle.Index];
        audioDevice.AudioSourceSetCursor(state.SourcePtr, cursor);

        OpenState(ref state);
        state.Cursor = cursor;
        CloseState(ref state);
    }
    public float GetCursor(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0;

        return audioSourceStates[handle.Index].Cursor;
    }
    public float GetLength(AudioSource handle)
    {
        if (!AudioSourceValid(handle))
            return 0;

        return audioSourceStates[handle.Index].Length;
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