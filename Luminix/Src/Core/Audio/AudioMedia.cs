using System;
using System.Runtime.CompilerServices;
using Luminix.Exceptions;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class AudioMedia : Workspace.Service
{
    internal static AudioMedia CreateCore()
    {
        var audioEngine = new AudioEngine();
        var audioMedia = new AudioMedia()
        {
            AudioEngine = audioEngine,  
        };
        
        return audioMedia;
    }
    public AudioMedia()
    {
        AudioEngine = ThreadBridges.GetGlobalBridge<AudioBridge>();
    }

    internal IAudioEngine AudioEngine { get; private init; }

    private AudioEngine GetAudioEngine([CallerMemberName] string operation = "")
    {
        if (AudioEngine is not AudioEngine audioEngine)
            throw new UnavailableAudioEngine(operation);
        
        return audioEngine;
    }

    // <================================= LISTENER =================================> \\

    public void SetListenerPosition(Vector3 value) => GetAudioEngine().SetListenerPosition(value.X, value.Y, value.Z);
    public void SetListenerVelocity(Vector3 value) => GetAudioEngine().SetListenerVelocity(value.X, value.Y, value.Z);

    public Vector3 GetListenerPosition() => AudioEngine.GetListenerPosition();
    public Vector3 GetListenerVelocity() => AudioEngine.GetListenerVelocity();

    // <================================= AUDIO CLIP =================================> \\

    public AudioClip GetClip(AudioAsset audioAsset) => GetAudioEngine().GetOrCreateClip(audioAsset.PCMData, audioAsset.AudioFormat, audioAsset.SampleRate);
    public AudioClip GetClip(byte[] pcmData, AudioClip.Format audioFormat, AudioClip.SampleRate sampleRate) => GetAudioEngine().GetOrCreateClip(pcmData, audioFormat, sampleRate);
    public void DisposeClip(AudioClip audio) => GetAudioEngine().DisposeClip(audio);
    public void DisposeClip(AudioResource audio) => GetAudioEngine().DisposeClip(audio.Clip);

    // <================================= AUDIO GROUPS =================================> \\

    public AudioGroup CreateGroup(string name) => GetAudioEngine().CreateGroup(name, new AudioGroup());
    public AudioGroup CreateGroup(string name, AudioGroup parent) => GetAudioEngine().CreateGroup(name, parent);

    public ReadOnlySpan<AudioGroup> GetGroups() => AudioEngine.GetGroups();
    public AudioGroup GetGroup(string name) => AudioEngine.GetGroup(name);

    public string GetGroupName(AudioGroup group) => AudioEngine.GetGroupName(group);
    public float GetGroupVolume(AudioGroup group) => AudioEngine.GetGroupVolume(group);

    public void SetGroupVolume(AudioGroup group, float volume) => AudioEngine.SetGroupVolume(group, volume);

    public bool TryGetGroupInfo(AudioGroup group, out AudioGroupInfo info) => AudioEngine.TryGetGroupInfo(group, out info);

    // <================================= AUDIO SOURCE =================================> \\

    public AudioSource CreateSource(AudioClip audio) => GetAudioEngine().CreateSource(audio);
    public AudioSource CreateSource(AudioResource audio) => GetAudioEngine().CreateSource(audio.Clip);
    public AudioSource CreateSource(byte[] pcmData, AudioClip.Format bufferFormat, AudioClip.SampleRate sampleRate) => GetAudioEngine().CreateSource(GetClip(pcmData, bufferFormat, sampleRate));
    public void DisposeSource(AudioSource audioSource) => GetAudioEngine().DisposeSource(audioSource);
    public void DisposeAllSources() => GetAudioEngine().DisposeAllSources();

    public void PlaySource(AudioSource handle) => AudioEngine.PlaySource(handle);
    public void StopSource(AudioSource handle) => AudioEngine.StopSource(handle);
    public void ResumeSource(AudioSource handle) => AudioEngine.ResumeSource(handle);
    public void PauseSource(AudioSource handle) => AudioEngine.PauseSource(handle);

    public void SetSourcePosition(AudioSource handle, Vector3 position) => AudioEngine.SetSourcePosition(handle, position);
    public void SetSourceSpatial(AudioSource handle, bool spatial) => AudioEngine.SetSourceSpatial(handle, spatial);
    public void SetSourceLooping(AudioSource handle, bool loop) => AudioEngine.SetSourceLooping(handle, loop);
    public void SetSourceVolume(AudioSource handle, float volume) => AudioEngine.SetSourceVolume(handle, volume);
    public void SetSourcePitch(AudioSource handle, float pitch) => AudioEngine.SetSourcePitch(handle, pitch);
    public void SetSourcePan(AudioSource handle, float pan) => AudioEngine.SetSourcePan(handle, pan);
    public void SetSourceRolloff(AudioSource handle, float rolloff) => AudioEngine.SetSourceRolloff(handle, rolloff);
    public void SetSourceMinDistance(AudioSource handle, float distance) => AudioEngine.SetSourceMinDistance(handle, distance);
    public void SetSourceMaxDistance(AudioSource handle, float distance) => AudioEngine.SetSourceMaxDistance(handle, distance);
    public void SetSourceCursor(AudioSource handle, float cursor) => AudioEngine.SetSourceCursor(handle, cursor);
    public void SetSourceAttenuationModel(AudioSource handle, int attenuationModel) => AudioEngine.SetSourceAttenuationModel(handle, attenuationModel);
    
    public bool IsSourcePlaying(AudioSource handle) => AudioEngine.IsSourcePlaying(handle);
    public bool IsSourceLooping(AudioSource handle) => AudioEngine.IsSourceLooping(handle);
    public bool IsSourceSpatial(AudioSource handle) => AudioEngine.IsSourceSpatial(handle);

    public Vector3 GetSourcePosition(AudioSource handle) => AudioEngine.GetSourcePosition(handle);
    public float GetSourceVolume(AudioSource handle) => AudioEngine.GetSourceVolume(handle);
    public float GetSourcePitch(AudioSource handle) => AudioEngine.GetSourcePitch(handle);
    public float GetSourcePan(AudioSource handle) => AudioEngine.GetSourcePan(handle);
    public float GetSourceRolloff(AudioSource handle) => AudioEngine.GetSourceRolloff(handle);
    public float GetSourceMinDistance(AudioSource handle) => AudioEngine.GetSourceMinDistance(handle);
    public float GetSourceMaxDistance(AudioSource handle) => AudioEngine.GetSourceMaxDistance(handle);
    public float GetSourceCursor(AudioSource handle) => AudioEngine.GetSourceCursor(handle);
    public float GetSourceLength(AudioSource handle) => AudioEngine.GetSourceLength(handle);
    public int GetSourceAttenuationModel(AudioSource handle) => AudioEngine.GetSourceAttenuationModel(handle); 

    public bool TryGetSourceInfo(AudioSource handle, out AudioSourceInfo audioSourceInfo) => AudioEngine.TryGetSourceInfo(handle, out audioSourceInfo);

}