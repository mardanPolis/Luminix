using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Luminix.Collections;
using Luminix.Exceptions;

namespace Luminix;

// <================================== AUDIO ENGINE - AUDIO GROUP ==================================>

internal sealed partial class AudioEngine : IAudioEngine, IDisposable
{
    private ConcurrentDictionary<string, AudioGroup> audioGroupsByName = new();
    private AudioGroup[] audioGroups = [];
    private AudioGroupState[] audioGroupStates = [];
    private int audioGroupCount;

    private void InitializeAudioGroups()
    {
        audioGroupsByName = new ConcurrentDictionary<string, AudioGroup>();
        audioGroups = new AudioGroup[InitialCapacity];
        audioGroupStates = new AudioGroupState[InitialCapacity];
        audioGroupCount = 0;

        audioBridge.SetGroupsByName(audioGroupsByName);
        audioBridge.SetGroups(audioGroups);
        audioBridge.SetStates(audioGroupStates);

        ThrowIfFailed(audioDevice.AudioGroupCreate(0, out var pMaster));
        AddGroup("Master", pMaster);
    }

    private AudioGroup AddGroup(string name, nint pointer)
    {
        if (audioGroupCount == audioGroups.Length)
        {
            var oldSize = audioGroups.Length;
            var newSize = Math.Max(InitialCapacity, audioGroupCount * 2);
            Array.Resize(ref audioGroups, newSize);
            Array.Resize(ref audioGroupStates, newSize);
            
            for(int i = oldSize; i < audioGroups.Length; i++)
            {
                
            }

            audioBridge.SetGroups(audioGroups);
            audioBridge.SetStates(audioGroupStates);
        }

        var group = new AudioGroup(audioGroupCount);
        audioGroupStates[audioGroupCount] = new AudioGroupState
        {
            Pointer = pointer,
            Name = name,
            Volume = 1.0f,
        };
        audioGroupsByName[name] = group;
        audioGroupCount++;
        return group;
    }

    private void SetStateField<T>(ref AudioGroupState state, ref T field, T value)
    {
        FenceState(ref state);
        field = value;
        FenceState(ref state);
    }
    private ref AudioGroupState GetState(AudioGroup group, [CallerMemberName] string operation = "")
    {
        if (!IsValid(group))
            throw new InvalidAudioGroupException(group, operation, nameof(group));
        return ref audioGroupStates[group.Index];
    }
    private void ReleaseState(ref AudioGroupState state)
    {
        if (!audioDevice.IsInvalid && state.Pointer != 0)
            audioDevice.AudioGroupDispose(state.Pointer);
        state.Pointer = 0;
    }
    private void FenceState(ref AudioGroupState sourceState)
    {
        sourceState.Version++;
        Thread.MemoryBarrier();
    }

    public bool IsValid(AudioGroup group)
    {
        ValidateDevice();
        return group.Index < audioGroupCount && audioGroupStates[group.Index].Pointer != 0;
    }

    public AudioGroup CreateGroup(string name, AudioGroup parent)
    {
        ValidateDevice();
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (audioGroupsByName.ContainsKey(name))
            throw new ArgumentException($"Failed to create audio group, reason: Audio group with name '{name}' already exists.", nameof(name));

        // Invalid parent falls back to master (index 0).
        var parentIndex = IsValid(parent) ? parent.Index : 0;
        var parentPtr = audioGroupStates[parentIndex].Pointer;

        ThrowIfFailed(audioDevice.AudioGroupCreate(parentPtr, out var pGroup));

        return AddGroup(name, pGroup);
    }
    public void DisposeAllGroups() // no DisposeAudioGroup(), because groups are exposed via ReadOnlySpan, so they live as long as the device
    {
        for (int i = audioGroupCount - 1; i >= 0; i--)
            ReleaseState(ref audioGroupStates[i]);

        audioGroupsByName.Clear();
        audioGroups = [];
        audioGroupStates = [];
        audioGroupCount = 0;
    }

    public ReadOnlySpan<AudioGroup> GetGroups()
    {
        ValidateDevice();
        return audioGroups.AsSpan(0, audioGroupCount);
    }
    public AudioGroup GetGroup(string name)
    {
        ValidateDevice();

        if (audioGroupsByName.TryGetValue(name, out var group))
            return group;

        throw new KeyNotFoundException($"Failed to find audio group with name '{name}'");
    }

    public void SetGroupVolume(AudioGroup group, float value)
    {
        ref var state = ref GetState(group);   // must be "ref var", otherwise you modify a copy
        audioDevice.AudioGroupSetVolume(state.Pointer, value);
        SetStateField(ref state, ref state.Volume, value);
    }

    public string GetGroupName(AudioGroup group)
    {
        return GetState(group).Name;
    }
    public float GetGroupVolume(AudioGroup group)
    {
        return GetState(group).Volume;
    }

    public bool TryGetGroupInfo(AudioGroup group, out AudioGroupInfo info)
    {
        info = new();
        if (!IsValid(group))
            return false;
        
        info = new AudioGroupInfo(in audioGroupStates[group.Index], false);
        return true;
    }
}