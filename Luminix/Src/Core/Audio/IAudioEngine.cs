using System;
using Microsoft.Xna.Framework;

namespace Luminix;

internal interface IAudioEngine
{
    public bool IsValid(AudioGroup group);
    public bool IsValid(AudioSource source);

    // <================================= LISTENER =================================> \\

    public Vector3 GetListenerPosition();
    public Vector3 GetListenerVelocity();

    public bool TryGetListenerInfo(out AudioListenerInfo info);

    // <================================= AUDIO GROUPS =================================> \\

    public ReadOnlySpan<AudioGroup> GetGroups();
    public AudioGroup GetGroup(string name);

    public void SetGroupVolume(AudioGroup group, float volume);

    public string GetGroupName(AudioGroup group);
    public float GetGroupVolume(AudioGroup group);

    public bool TryGetGroupInfo(AudioGroup group, out AudioGroupInfo info);

    // <================================= AUDIO SOURCE =================================> \\

    public void PlaySource(AudioSource source);
    public void StopSource(AudioSource source);
    public void ResumeSource(AudioSource source);
    public void PauseSource(AudioSource source);

    public void SetSourcePosition(AudioSource source, Vector3 position);
    public void SetSourceVelocity(AudioSource source, Vector3 velocity);
    public void SetSourceSpatial(AudioSource source, bool spatial);
    public void SetSourceLooping(AudioSource source, bool loop);
    public void SetSourceVolume(AudioSource source, float volume);
    public void SetSourcePitch(AudioSource source, float pitch);
    public void SetSourcePan(AudioSource source, float pan);
    public void SetSourceRolloff(AudioSource source, float rolloff);
    public void SetSourceMinDistance(AudioSource source, float distance);
    public void SetSourceMaxDistance(AudioSource source, float distance);
    public void SetSourceCursor(AudioSource source, float cursor);
    public void SetSourceAttenuationModel(AudioSource source, int attenuationModel);

    public bool IsSourcePlaying(AudioSource source);
    public bool IsSourceSpatial(AudioSource source);
    public bool IsSourceLooping(AudioSource source);

    public Vector3 GetSourcePosition(AudioSource source);
    public Vector3 GetSourceVelocity(AudioSource source);
    public float GetSourceVolume(AudioSource source);
    public float GetSourcePitch(AudioSource source);
    public float GetSourcePan(AudioSource source);
    public float GetSourceRolloff(AudioSource source);
    public float GetSourceMinDistance(AudioSource source);
    public float GetSourceMaxDistance(AudioSource source);
    public float GetSourceCursor(AudioSource source);
    public float GetSourceLength(AudioSource source);
    public int GetSourceAttenuationModel(AudioSource source);

    public bool TryGetSourceInfo(AudioSource source, out AudioSourceInfo info);
}