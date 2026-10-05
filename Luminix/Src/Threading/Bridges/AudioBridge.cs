using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Luminix.Exceptions;
using Luminix.Threading.Bridges;
using Luminix.Threading.Commands;
using Microsoft.Xna.Framework;

namespace Luminix;

internal sealed class AudioBridge : IAudioEngine
{
    internal CommandBridge<AudioCommand> Commands { get; } = new();

    // <================================= LISTENER =================================> \\

    private volatile AudioListenerState[] listenerStates;

    public void SetStates(AudioListenerState[] states) => listenerStates = states;

    private ref AudioListenerState GetState()
    {
        var states = listenerStates;
        if (states == null || states.Length == 0)
            throw new InvalidOperationException("Audio listener is not initialized.");

        return ref states[0];
    }

    public Vector3 GetListenerPosition()
    {
        var info = new AudioListenerInfo(in GetState(), true);
        return new Vector3(info.PosX, info.PosY, info.PosZ);
    }
    public Vector3 GetListenerVelocity()
    {
        var info = new AudioListenerInfo(in GetState(), true);
        return new Vector3(info.VelX, info.VelY, info.VelZ);
    }

    public bool TryGetListenerInfo(out AudioListenerInfo info)
    {
        var states = listenerStates;
        if (states == null || states.Length == 0)
        {
            info = default;
            return false;
        }

        info = new AudioListenerInfo(in states[0], true);
        return true;
    }

    // <================================= AUDIO GROUPS =================================> \\

    private volatile ConcurrentDictionary<string, AudioGroup> groupsByName;
    private volatile AudioGroup[] groups;
    private volatile AudioGroupState[] groupStates;
    private volatile int groupCount;

    public void SetGroupsByName(ConcurrentDictionary<string, AudioGroup> groupsByName) => this.groupsByName = groupsByName;
    public void SetGroups(AudioGroup[] groups) => this.groups = groups;
    public void SetStates(AudioGroupState[] states) => groupStates = states;
    public void SetGroupCount(int count) => groupCount = count;

    public bool IsValid(AudioGroup group)
    {
        var states = groupStates;
        if (states == null) return false;

        return (uint)group.Index < (uint)states.Length
            && Volatile.Read(ref states[group.Index].Pointer) != 0;
    }

    private ref AudioGroupState GetState(AudioGroup group, [CallerMemberName] string operation = "")
    {
        if (!IsValid(group))
            throw new InvalidAudioGroupException(group, operation, nameof(group));

        return ref groupStates[group.Index];
    }

    private void Enqueue(AudioCommandType type, AudioGroup group, float x = 0, float y = 0, float z = 0, [CallerMemberName] string operation = "")
    {
        if (!IsValid(group))
            throw new InvalidAudioGroupException(group, operation, nameof(group));

        Commands.NewCommand(new AudioCommand(type, group.Index, x, y, z));
    }

    public ReadOnlySpan<AudioGroup> GetGroups()
    {
        var count = groupCount;   // count first, then array (engine publishes array first, count second)
        var array = groups;
        if (array == null) return default;

        return array.AsSpan(0, Math.Min(count, array.Length));
    }

    public AudioGroup GetGroup(string name) =>
        groupsByName.TryGetValue(name, out var group)
            ? group
            : throw new KeyNotFoundException($"Failed to find audio group with name '{name}'");

    public void SetGroupVolume(AudioGroup group, float volume) => Enqueue(AudioCommandType.SetGroupVolume, group, volume);

    public string GetGroupName(AudioGroup group) => Volatile.Read(ref GetState(group).Name);
    public float GetGroupVolume(AudioGroup group) => Volatile.Read(ref GetState(group).Volume);

    public bool TryGetGroupInfo(AudioGroup group, out AudioGroupInfo info)
    {
        info = default;
        if (!IsValid(group))
            return false;

        info = new AudioGroupInfo(in groupStates[group.Index], true);
        return true;
    }

    // <================================= AUDIO SOURCES =================================> \\

    private volatile AudioSourceState[] sourceStates;

    public void SetStates(AudioSourceState[] states) => sourceStates = states;

    public bool IsValid(AudioSource source)
    {
        var states = sourceStates;
        if (states == null) return false;

        return source.Index != 0
            && (uint)source.Index < (uint)states.Length
            && Volatile.Read(ref states[source.Index].Pointer) != 0;
    }

    private ref AudioSourceState GetState(AudioSource source, [CallerMemberName] string operation = "")
    {
        if (!IsValid(source))
            throw new InvalidAudioSourceException(source, operation, nameof(source));

        return ref sourceStates[source.Index];
    }

    private void Enqueue(AudioCommandType type, AudioSource source, float x = 0, float y = 0, float z = 0, [CallerMemberName] string operation = "")
    {
        if (!IsValid(source))
            throw new InvalidAudioSourceException(source, operation, nameof(source));

        Commands.NewCommand(new AudioCommand(type, source.Index, x, y, z, source.Generation));
    }

    public void PlaySource(AudioSource source) => Enqueue(AudioCommandType.PlaySource, source);
    public void StopSource(AudioSource source) => Enqueue(AudioCommandType.StopSource, source);
    public void ResumeSource(AudioSource source) => Enqueue(AudioCommandType.ResumeSource, source);
    public void PauseSource(AudioSource source) => Enqueue(AudioCommandType.PauseSource, source);

    public void SetSourcePosition(AudioSource source, Vector3 position) => Enqueue(AudioCommandType.SetSourcePosition, source, position.X, position.Y, position.Z);
    public void SetSourceVelocity(AudioSource source, Vector3 velocity) => Enqueue(AudioCommandType.SetSourceVelocity, source, velocity.X, velocity.Y, velocity.Z);
    public void SetSourceSpatial(AudioSource source, bool spatial) => Enqueue(AudioCommandType.SetSourceSpatial, source, spatial ? 1f : 0f);
    public void SetSourceLooping(AudioSource source, bool loop) => Enqueue(AudioCommandType.SetSourceLooping, source, loop ? 1f : 0f);
    public void SetSourceVolume(AudioSource source, float volume) => Enqueue(AudioCommandType.SetSourceVolume, source, volume);
    public void SetSourcePitch(AudioSource source, float pitch) => Enqueue(AudioCommandType.SetSourcePitch, source, pitch);
    public void SetSourcePan(AudioSource source, float pan) => Enqueue(AudioCommandType.SetSourcePan, source, pan);
    public void SetSourceRolloff(AudioSource source, float rolloff) => Enqueue(AudioCommandType.SetSourceRolloff, source, rolloff);
    public void SetSourceMinDistance(AudioSource source, float distance) => Enqueue(AudioCommandType.SetSourceMinDistance, source, distance);
    public void SetSourceMaxDistance(AudioSource source, float distance) => Enqueue(AudioCommandType.SetSourceMaxDistance, source, distance);
    public void SetSourceCursor(AudioSource source, float cursor) => Enqueue(AudioCommandType.SetSourceCursor, source, cursor);
    public void SetSourceAttenuationModel(AudioSource source, int attenuationModel) => Enqueue(AudioCommandType.SetSourceAttenuationModel, source, attenuationModel);

    public bool IsSourcePlaying(AudioSource source) => Volatile.Read(ref GetState(source).State) == 1;
    public bool IsSourceSpatial(AudioSource source) => Volatile.Read(ref GetState(source).IsSpatial);
    public bool IsSourceLooping(AudioSource source) => Volatile.Read(ref GetState(source).IsLooping);
    public float GetSourceVolume(AudioSource source) => Volatile.Read(ref GetState(source).Volume);
    public float GetSourcePitch(AudioSource source) => Volatile.Read(ref GetState(source).Pitch);
    public float GetSourcePan(AudioSource source) => Volatile.Read(ref GetState(source).Pan);
    public float GetSourceRolloff(AudioSource source) => Volatile.Read(ref GetState(source).Rolloff);
    public float GetSourceMinDistance(AudioSource source) => Volatile.Read(ref GetState(source).MinDistance);
    public float GetSourceMaxDistance(AudioSource source) => Volatile.Read(ref GetState(source).MaxDistance);
    public float GetSourceCursor(AudioSource source) => Volatile.Read(ref GetState(source).Cursor);
    public int GetSourceAttenuationModel(AudioSource source) => Volatile.Read(ref GetState(source).AttenuationModel);
    public float GetSourceLength(AudioSource source) => GetState(source).Duration;

    public Vector3 GetSourcePosition(AudioSource source)
    {
        ref var state = ref GetState(source);
        Vector3 value;
        int v1, v2;
        do
        {
            v1 = state.Version;
            value = state.Position;
            Thread.MemoryBarrier();
            v2 = state.Version;
        }
        while (v1 != v2 || (v1 & 1) != 0);
        return value;
    }

    public Vector3 GetSourceVelocity(AudioSource source)
    {
        ref var state = ref GetState(source);
        Vector3 value;
        int v1, v2;
        do
        {
            v1 = state.Version;
            value = state.Velocity;
            Thread.MemoryBarrier();
            v2 = state.Version;
        }
        while (v1 != v2 || (v1 & 1) != 0);
        return value;
    }

    public bool TryGetSourceInfo(AudioSource source, out AudioSourceInfo info)
    {
        info = default;
        if (!IsValid(source))
            return false;

        info = new AudioSourceInfo(in sourceStates[source.Index], true);
        return true;
    }
}