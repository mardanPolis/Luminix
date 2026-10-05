using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Luminix.Collections;
using Luminix.Exceptions;
using Luminix.Native.Audio;
using Microsoft.Xna.Framework;

namespace Luminix;

// <================================== AUDIO ENGINE - AUDIO SOURCE ==================================>

internal sealed partial class AudioEngine : IAudioEngine, IDisposable
{
    private int audioSourceCount;
    private int audioLastIndex;
    private int audioGenerationCounter; 
    private Stack<int> freeIndices;
    private DenseFixedList<int> playingSourceIndexes;
    private AudioSourceState[] audioSourceStates;
    
    public int AudioSourceCount;
    public int AudioSourcePlayingCount => playingSourceIndexes.Count;

    private void InitializeAudioSources()
    {
        freeIndices = new(256);
        playingSourceIndexes = new(128);
        audioSourceStates = new AudioSourceState[256];

        audioBridge.SetStates(audioSourceStates);
    }

    private void SetStateField<T>(ref AudioSourceState state, ref T field, T value)
    {
        FenceState(ref state);
        field = value;
        FenceState(ref state);
    }
    private ref AudioSourceState GetState(AudioSource source, [CallerMemberName] string operation = "")
    {
        if (!IsValid(source))
            throw new InvalidAudioSourceException(source, operation, nameof(source));
 
        return ref audioSourceStates[source.Index];
    }
    private void ReleaseState(ref AudioSourceState state)
    {
        if (!audioDevice.IsInvalid)
            audioDevice.AudioSourceDispose(state.Pointer);

        FenceState(ref state);
        state.Pointer = 0;
        state.Generation = 0;
        FenceState(ref state);
    }
    private void FenceState(ref AudioSourceState sourceState)
    {
        sourceState.Version++;
        Thread.MemoryBarrier();
    }

    public bool IsValid(AudioSource source)
    {
        ValidateDevice();

        return source.Generation != 0
            && (uint)source.Index < (uint)audioSourceStates.Length
            && audioSourceStates[source.Index].Generation == source.Generation;
    }

    public void UpdatePlayingAudioSources()
    {
        ValidateDevice();

        for (int i = playingSourceIndexes.Count - 1; i >= 0; i--)
        {
            int index = playingSourceIndexes[i];
            ref var state = ref audioSourceStates[index];

            if (state.Pointer == 0)
            {
                playingSourceIndexes.Remove(index);
                continue;
            }

            if (audioDevice.AudioSourceGetCursor(state.Pointer, out float cursor) != AudioDeviceResult.Success)
                continue;

            bool finished = audioDevice.AudioSourceAtEnd(state.Pointer);

            FenceState(ref state);
            state.Cursor = cursor;
            if (finished)
                state.State = 0; // non-looping source reached the end
            FenceState(ref state);

            if (finished)
                playingSourceIndexes.Remove(index);
        }
    }

    public AudioSource CreateSource(AudioClip audio)
    {
        ValidateDevice();

        if(audio.IsDisposed)
            throw new ArgumentException("Cannot create AudioSource instance from the provided AudioClip, reason: Audio is disposed.");
        
        ThrowIfFailed(audioDevice.AudioSourceNew(audio.ClipPointer, out IntPtr pSource));
            
        audioSourceCount++;

        if (!freeIndices.TryPop(out var index)) {
            audioLastIndex++;
            index = audioLastIndex;

            if (audioSourceStates.Length <= audioLastIndex)
            {
                Array.Resize(ref audioSourceStates, Math.Max(4, audioSourceStates.Length * 2));
                audioBridge.SetStates(audioSourceStates);
            }
        }

        var generation = ++audioGenerationCounter;
        if (generation == 0) generation = ++audioGenerationCounter;

        int version = audioSourceStates[index].Version;

        var state = new AudioSourceState(pSource, audio.Duration) { Generation = generation };
        state.Version = version + 2;   // stays even
        audioSourceStates[index] = state;
        return new AudioSource(index, generation);
    }
    public void DisposeSource(AudioSource source)
    {
        if (!IsValid(source))
            return;
    
        ReleaseState(ref audioSourceStates[source.Index]);
    
        playingSourceIndexes.Remove(source.Index);
        freeIndices.Push(source.Index);
        audioSourceCount--;
    }
    public void DisposeAllSources()
    {
        for (int index = 1; index <= audioLastIndex; index++)
        {
            ref var state = ref audioSourceStates[index];
            if (state.Pointer == 0)
                continue;
    
            ReleaseState(ref state);
        }
    
        playingSourceIndexes.Clear();
        freeIndices.Clear();
        freeIndices.TrimExcess(InitialCapacity);
        audioSourceStates = new AudioSourceState[InitialCapacity];
        audioLastIndex = 0;
        audioSourceCount = 0;
    }

    public void PlaySource(AudioSource source)
    {
        ref var state = ref GetState(source);
        if(!playingSourceIndexes.TryAdd(source.Index, out int index) && index == -1)
        {
            Logger.NewLog(Logger.Log.Level.Warning, $"Unable to play an audio source {source}, reason: Reached max concurrent playing sources(128).");
            return;
        }

        ThrowIfFailed(audioDevice.AudioSourcePlay(state.Pointer));
        SetStateField(ref state, ref state.State, 1);
    }
    public void ResumeSource(AudioSource source)
    {
        ref var state = ref GetState(source);
        if(!playingSourceIndexes.TryAdd(source.Index, out int index) && index == -1)
        {
            Logger.NewLog(Logger.Log.Level.Warning, $"Unable to resume an audio source {source}, reason: Reached max concurrent playing sources(128).");
            return;
        }

        ThrowIfFailed(audioDevice.AudioSourceResume(state.Pointer));
        SetStateField(ref state, ref state.State, 1);
    }
    public void StopSource(AudioSource source)
    {
        ref var state = ref GetState(source);
        ThrowIfFailed(audioDevice.AudioSourceStop(state.Pointer));

        playingSourceIndexes.Remove(source.Index);
        SetStateField(ref state, ref state.State, 0);
    }
    public void PauseSource(AudioSource source)
    {
        ref var state = ref GetState(source);
        ThrowIfFailed(audioDevice.AudioSourcePause(state.Pointer));
        
        playingSourceIndexes.Remove(source.Index);
        SetStateField(ref state, ref state.State, 0);
    }

    public void SetSourcePosition(AudioSource source, Vector3 value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetPosition(state.Pointer, value.X, value.Y, value.Z);
        SetStateField(ref state, ref state.Position, value);
    }
    public void SetSourceVelocity(AudioSource source, Vector3 value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetVelocity(state.Pointer, value.X, value.Y, value.Z);
        SetStateField(ref state, ref state.Velocity, value);
    }
    public void SetSourceVolume(AudioSource source, float value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetVolume(state.Pointer, value);
        SetStateField(ref state, ref state.Volume, value);
    }
    public void SetSourcePitch(AudioSource source, float value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetPitch(state.Pointer, value);
        SetStateField(ref state, ref state.Pitch, value);
    }
    public void SetSourcePan(AudioSource source, float value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetPan(state.Pointer, value);
        SetStateField(ref state, ref state.Pan, value);
    }
    public void SetSourceRolloff(AudioSource source, float value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetRolloff(state.Pointer, value);
        SetStateField(ref state, ref state.Rolloff, value);
    }
    public void SetSourceMinDistance(AudioSource source, float value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetMinDistance(state.Pointer, value);
        SetStateField(ref state, ref state.MinDistance, value);
    }
    public void SetSourceMaxDistance(AudioSource source, float value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetMaxDistance(state.Pointer, value);
        SetStateField(ref state, ref state.MaxDistance, value);
    }
    public void SetSourceCursor(AudioSource source, float value)
    {
        ref var state = ref GetState(source);
        ThrowIfFailed(audioDevice.AudioSourceSetCursor(state.Pointer, value));
        SetStateField(ref state, ref state.Cursor, value);
    }
    public void SetSourceAttenuationModel(AudioSource source, int value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetAttenuationModel(state.Pointer, value);
        SetStateField(ref state, ref state.AttenuationModel, value);
    }
    public void SetSourceSpatial(AudioSource source, bool value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetSpatial(state.Pointer, value);
        SetStateField(ref state, ref state.IsSpatial, value);
    }
    public void SetSourceLooping(AudioSource source, bool value)
    {
        ref var state = ref GetState(source);
        audioDevice.AudioSourceSetLooping(state.Pointer, value);
        SetStateField(ref state, ref state.IsLooping, value);
    }
    
    public Vector3 GetSourcePosition(AudioSource source) => GetState(source).Position;
    public Vector3 GetSourceVelocity(AudioSource source) => GetState(source).Velocity;
    public float GetSourceVolume(AudioSource source) => GetState(source).Volume;
    public float GetSourcePitch(AudioSource source) => GetState(source).Pitch;
    public float GetSourcePan(AudioSource source) => GetState(source).Pan;
    public float GetSourceRolloff(AudioSource source) => GetState(source).Rolloff;
    public float GetSourceMinDistance(AudioSource source) => GetState(source).MinDistance;
    public float GetSourceMaxDistance(AudioSource source) => GetState(source).MaxDistance;
    public float GetSourceCursor(AudioSource source) => GetState(source).Cursor;
    public float GetSourceLength(AudioSource source) => GetState(source).Duration;
    public int GetSourceAttenuationModel(AudioSource source) => GetState(source).AttenuationModel;
 
    public bool IsSourcePlaying(AudioSource source) => GetState(source).State == 1;
    public bool IsSourceSpatial(AudioSource source) => GetState(source).IsSpatial;
    public bool IsSourceLooping(AudioSource source) => GetState(source).IsLooping;

    public bool TryGetSourceInfo(AudioSource source, out AudioSourceInfo info)
    {
        info = default;
        if (!IsValid(source))
            return false;
        
        info = new AudioSourceInfo(in audioSourceStates[source.Index], false);
        return true;
    }
}