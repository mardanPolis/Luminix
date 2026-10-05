using System;
using System.Runtime.InteropServices;

namespace Luminix.Native.Audio;

[StructLayout(LayoutKind.Sequential)]
internal struct MaVec3
{
    public float X;
    public float Y;
    public float Z;
}

internal sealed partial class AudioDevice : SafeHandle, IDisposable
{
    private const string lib = "miniaudio";
    
    // ---- Engine --------------------------------------------------------------

    [LibraryImport(lib)] private static partial AudioDeviceResult ma_create_engine(out IntPtr pDevice);
    [LibraryImport(lib)] private static partial void ma_dispose_engine(IntPtr pConfig);

    [LibraryImport(lib)] private static partial AudioDeviceResult ma_engine_start   (IntPtr pDevice);
    [LibraryImport(lib)] private static partial AudioDeviceResult ma_engine_stop    (IntPtr pDevice);

    public static AudioDevice Create()
    {
        var result = ma_create_engine(out IntPtr pDevice);
        if(result != AudioDeviceResult.Success)
            throw new InvalidOperationException($"Failed to initialize MiniAudio engine. Error code: {result}");

        AudioDevice device = new();
        device.handle = pDevice;

        return device;
    }
    private AudioDevice() : base(IntPtr.Zero, true) {}

    public override bool IsInvalid => handle == IntPtr.Zero;

    public void Start() => ma_engine_start(handle);
    public void Stop() => ma_engine_stop(handle);

    // ---- Audio Group --------------------------------------------------------------

    [LibraryImport(lib)] private static partial AudioDeviceResult ma_create_sound_group (IntPtr pDevice, IntPtr pParentGroup, out IntPtr pGroup);
    [LibraryImport(lib)] private static partial AudioDeviceResult ma_dispose_sound_group(IntPtr pGroup);
    [LibraryImport(lib)] private static partial uint ma_sound_group_set_vol             (IntPtr pGroup, float volume);

    public AudioDeviceResult AudioGroupCreate(IntPtr pParentGroup, out IntPtr pGroup) => ma_create_sound_group(handle, pParentGroup, out pGroup);
    public AudioDeviceResult AudioGroupDispose(IntPtr pGroup) => ma_dispose_sound_group(pGroup);
    public uint AudioGroupSetVolume(IntPtr pGroup, float volume) => ma_sound_group_set_vol(pGroup, volume);

    // ---- Listener --------------------------------------------------------------

    [LibraryImport(lib)] private static partial void ma_engine_listener_set_position    (IntPtr pDevice, uint listener, float x, float y, float z);
    [LibraryImport(lib)] private static partial void ma_engine_listener_set_velocity    (IntPtr pDevice, uint listener, float x, float y, float z);
    [LibraryImport(lib)] private static partial void ma_engine_listener_set_direction   (IntPtr pDevice, uint listener, float x, float y, float z);
    [LibraryImport(lib)] private static partial void ma_engine_listener_set_world_up    (IntPtr pDevice, uint listener, float x, float y, float z);

    public void ListenerSetPosition(float x, float y, float z) => ma_engine_listener_set_position(handle, 0, x, y, z);
    public void ListenerSetVelocity(float x, float y, float z) => ma_engine_listener_set_velocity(handle, 0, x, y, z);
    public void ListenerSetDirection(float x, float y, float z) => ma_engine_listener_set_direction(handle, 0, x, y, z);
    public void ListenerSetWorldUp(float x, float y, float z) => ma_engine_listener_set_world_up(handle, 0, x, y, z);


    // ---- clip --------------------------------------------------------------

    [LibraryImport(lib)] private static partial AudioDeviceResult ma_create_audio_clip  (ReadOnlySpan<byte> pPCM, ulong frameCount, uint channels, AudioClip.SampleFormat format, uint sampleRate, out IntPtr pClip); // MUST CHECK
    [LibraryImport(lib)] private static partial void ma_dispose_audio_clip              (IntPtr pClip);  // MUST CHECK

    public AudioDeviceResult AudioClipNew(ReadOnlySpan<byte> pcmData, ulong frameCount, AudioClip.Channels channels, AudioClip.SampleFormat sampleFormat, AudioClip.SampleRate sampleRate, out IntPtr pClip) => ma_create_audio_clip(pcmData, frameCount, (uint)channels, sampleFormat, (uint)sampleRate, out pClip);
    public void AudioClipDispose(IntPtr pClip) => ma_dispose_audio_clip(pClip);

    // ---- source --------------------------------------------------------------

    [LibraryImport(lib)] private static partial AudioDeviceResult ma_create_audio_source(IntPtr pDevice, IntPtr pClip, out IntPtr pSource);  // MUST CHECK
    [LibraryImport(lib)] private static partial void ma_dispose_audio_source            (IntPtr pSource);  // MUST CHECK

    [LibraryImport(lib)] private static partial AudioDeviceResult ma_audio_source_play  (IntPtr pSource);
    [LibraryImport(lib)] private static partial AudioDeviceResult ma_audio_source_stop  (IntPtr pSource);
    [LibraryImport(lib)] private static partial AudioDeviceResult ma_audio_source_resume(IntPtr pSource);
    [LibraryImport(lib)] private static partial AudioDeviceResult ma_audio_source_pause (IntPtr pSource);

    [LibraryImport(lib)] private static partial void ma_sound_set_volume(IntPtr pSource, float volume);
    [LibraryImport(lib)] private static partial void ma_sound_set_pitch (IntPtr pSource, float pitch);
    [LibraryImport(lib)] private static partial void ma_sound_set_pan   (IntPtr pSource, float pan);

    [LibraryImport(lib)] private static partial void ma_sound_set_looping(IntPtr pSource, int isLooping);

    [LibraryImport(lib)] private static partial void ma_sound_set_position              (IntPtr pSource, float x, float y, float z);
    [LibraryImport(lib)] private static partial void ma_sound_set_velocity              (IntPtr pSource, float x, float y, float z);
    [LibraryImport(lib)] private static partial void ma_sound_set_attenuation_model     (IntPtr pSource, int attenuationModel);
    [LibraryImport(lib)] private static partial void ma_sound_set_rolloff               (IntPtr pSource, float rolloff);
    [LibraryImport(lib)] private static partial void ma_sound_set_min_distance          (IntPtr pSource, float minDistance);
    [LibraryImport(lib)] private static partial void ma_sound_set_max_distance          (IntPtr pSource, float maxDistance);
    [LibraryImport(lib)] private static partial void ma_sound_set_spatialization_enabled(IntPtr pSource, int enabled);

    [LibraryImport(lib)] private static partial int ma_sound_at_end(IntPtr pSource);

    [LibraryImport(lib, EntryPoint = "ma_sound_seek_to_second")]        private static partial AudioDeviceResult ma_sound_set_cursor(IntPtr pSource, float second);
    [LibraryImport(lib, EntryPoint = "ma_sound_get_cursor_in_seconds")] private static partial AudioDeviceResult ma_sound_get_cursor(IntPtr pSource, out float pCursor);
    [LibraryImport(lib, EntryPoint = "ma_sound_get_length_in_seconds")] private static partial AudioDeviceResult ma_sound_get_length(IntPtr pSource, out float pLength);

    public AudioDeviceResult AudioSourceNew(IntPtr pClip, out IntPtr pSource) => ma_create_audio_source(handle, pClip, out pSource);
    public void AudioSourceDispose(IntPtr pSource) => ma_dispose_audio_source(pSource);

    public AudioDeviceResult AudioSourcePlay(IntPtr pSource)    => ma_audio_source_play(pSource);
    public AudioDeviceResult AudioSourceStop(IntPtr pSource)    => ma_audio_source_stop(pSource);
    public AudioDeviceResult AudioSourceResume(IntPtr pSource)  => ma_audio_source_resume(pSource);
    public AudioDeviceResult AudioSourcePause(IntPtr pSource)   => ma_audio_source_pause(pSource);

    public void AudioSourceSetVolume(IntPtr pSource, float volume) => ma_sound_set_volume(pSource, volume);
    public void AudioSourceSetPitch(IntPtr pSource, float pitch) => ma_sound_set_pitch(pSource, pitch);
    public void AudioSourceSetPan(IntPtr pSource, float pan) => ma_sound_set_pan(pSource, pan);

    public void AudioSourceSetSpatial(IntPtr pSource, bool spatial) => ma_sound_set_spatialization_enabled(pSource, spatial ? 1 : 0);
    public void AudioSourceSetLooping(IntPtr pSource, bool looping) => ma_sound_set_looping(pSource, looping ? 1 : 0);

    public void AudioSourceSetPosition(IntPtr pSource, float x, float y, float z) => ma_sound_set_position(pSource, x, y, z);
    public void AudioSourceSetVelocity(IntPtr pSource, float x, float y, float z) => ma_sound_set_velocity(pSource, x, y, z);
    public void AudioSourceSetAttenuationModel(IntPtr pSource, int attenuationModel) => ma_sound_set_attenuation_model(pSource, attenuationModel);

    public void AudioSourceSetRolloff(IntPtr pSource, float rolloff) => ma_sound_set_rolloff(pSource, rolloff);
    public void AudioSourceSetMinDistance(IntPtr pSource, float distance) => ma_sound_set_min_distance(pSource, distance);
    public void AudioSourceSetMaxDistance(IntPtr pSource, float distance) => ma_sound_set_max_distance(pSource, distance);

    public AudioDeviceResult AudioSourceSetCursor(IntPtr pSource, float second) => ma_sound_set_cursor(pSource, second);
    public AudioDeviceResult AudioSourceGetCursor(IntPtr pSource, out float second) => ma_sound_get_cursor(pSource, out second);
    public AudioDeviceResult AudioSourceLength(IntPtr pSource, out float second) => ma_sound_get_length(pSource, out second);

    public bool AudioSourceAtEnd(IntPtr pSource) => ma_sound_at_end(pSource) == 1;

    protected override bool ReleaseHandle()
    {
        ma_dispose_engine(handle);
        return true;
    }

}
