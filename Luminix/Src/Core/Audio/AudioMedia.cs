using System;
using System.Net.Http.Headers;
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

    internal AudioEngine AudioEngine { get; private init; }
    
    // public AudioClip GetAudio(byte[] pcmData, BufferFormat bufferFormat, int sampleRate)
    // {
    //     if (AudioEngine is null)
    //         throw new InvalidOperationException("Cant get audio, reason: AudioEngine is not available");

    //     return AudioEngine.GetOrCreateAudio(pcmData, bufferFormat, sampleRate);
    // }

    // public void DisposeAudio(AudioClip audio)
    // {
    //     if (AudioEngine is null)
    //         throw new InvalidOperationException("Cant dispose audio, reason: AudioEngine is not available");

    //     AudioEngine.DisposeAudio(audio);
    // }

    // public Sound CreateSound(AudioClip audio)
    // {
    //     if (AudioEngine is null)
    //         throw new InvalidOperationException("Cant Create sound, reason: AudioEngine is not available");

    //     return AudioEngine.CreateSound(audio);
    // }
    // public Sound CreateSound(AudioResource audio)
    // {
    //     return CreateSound(audio.Clip);
    // }
    // public Sound CreateSound(byte[] pcmData, BufferFormat bufferFormat, int sampleRate)
    // {
    //     return CreateSound(GetAudio(pcmData, bufferFormat, sampleRate));
    // }
    
    // public void Play(Sound handle)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.Play(handle);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().Play(handle);
    // }
    // public void Stop(Sound handle)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.Stop(handle);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().Stop(handle);
    // }

    // public void Resume(Sound handle)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.Resume(handle);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().Resume(handle);
    // }
    // public void Pause(Sound handle)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.Pause(handle);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().Pause(handle);
    // }

    // public void SetSpatial(Sound handle, bool spatial)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.SetSpatial(handle, spatial);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().SetSpatial(handle, spatial);
    // }
    // public void SetLooping(Sound handle, bool loop)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.SetLooping(handle, loop);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().SetLooping(handle, loop);
    // }
    // public void SetVolume(Sound handle, float volume)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.SetVolume(handle, volume);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().SetVolume(handle, volume);
    // }
    // public void SetPitch(Sound handle, float pitch)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.SetPitch(handle, pitch);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().SetPitch(handle, pitch);
    // }
    // public void SetPan(Sound handle, float pan)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.SetPan(handle, pan);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().SetPan(handle, pan);
    // }
    // public void SetPosition(Sound handle, Vector2 position)
    // {
    //     if(AudioEngine is not null)
    //         AudioEngine.SetPosition(handle, position);
    //     else
    //         ThreadBridges.GetGlobalBridge<AudioBridge>().SetPosition(handle, position);
    // }

    // public bool TryGetInfo(Sound handle, out SoundInfo soundInfo)
    // {
    //     if(AudioEngine is not null)
    //         return AudioEngine.TryGetInfo(handle, out soundInfo);
    //     else
    //         return ThreadBridges.GetGlobalBridge<AudioBridge>().TryGetInfo(handle, out soundInfo);
    // }

    // public bool IsPlaying(Sound handle) => TryGetInfo(handle, out SoundInfo SoundInfo) && SoundInfo.IsPlaying;
    // public bool IsLooping(Sound handle) => TryGetInfo(handle, out SoundInfo SoundInfo) && SoundInfo.IsLooped;
    // public float GetVolume(Sound handle) => TryGetInfo(handle, out SoundInfo SoundInfo) ? SoundInfo.Volume : default;
    // public float GetPitch(Sound handle)  => TryGetInfo(handle, out SoundInfo SoundInfo) ? SoundInfo.Pitch : default;
    // public float GetPan(Sound handle)    => TryGetInfo(handle, out SoundInfo SoundInfo) ? SoundInfo.Pan : default;
    // public Vector2 GetPosition(Sound handle) => TryGetInfo(handle, out SoundInfo SoundInfo) ? SoundInfo.Position : default;
}